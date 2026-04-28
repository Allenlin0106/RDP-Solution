using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RdpSolution.DAL.Models;
using RdpSolution.UI.SessionClient;

namespace RdpSolution.UI.VncClient
{
    /// <summary>
    /// 修正版 Pure-.NET VNC 客戶端
    /// 支援 VNC Password (Type 2) 與 UltraVNC MS-Logon II (Type 17/113)
    /// </summary>
    public sealed class VncClientControl : UserControl, IRemoteControl
    {
        // ------------------------------------------------------------------ 狀態與欄位

        private TcpClient     _tcp;
        private NetworkStream _stream;
        private Thread        _receiveThread;

        private Bitmap _framebuffer;
        private readonly object _fbLock = new object();
        private int _fbW, _fbH;

        private volatile int _state;   // 0=停止, 1=已連線, 2=連線中
        private bool _intentionalDisconnect;

        private RemoteHostConfig _host;

        // ------------------------------------------------------------------ IRemoteControl 實作

        public int ConnectedState => _state;

        public event EventHandler Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<CreateFailedEventArgs> CreateFailed;

        // ------------------------------------------------------------------ 連線控制

        public void Connect(RemoteHostConfig host)
        {
            _host                  = host;
            _intentionalDisconnect = false;
            _state                 = 2;

            // 強制建立 Handle，確保背景執行緒觸發事件時 UI 已就緒
            var _ = Handle;

            _receiveThread = new Thread(ConnectAndReceive) { IsBackground = true };
            _receiveThread.Start();
        }

        public void Disconnect()
        {
            _intentionalDisconnect = true;
            _state = 0;
            try { _tcp?.Close(); } catch { }
        }

        // ------------------------------------------------------------------ 背景連線執行緒

        private void ConnectAndReceive()
        {
            try
            {
                _tcp = new TcpClient();
                
                // 設定連線逾時
                var connectResult = _tcp.BeginConnect(_host.Hostname, _host.Port, null, null);
                if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("連線逾時：無法連接到遠端伺服器。");
                }
                _tcp.EndConnect(connectResult);

                // 設定讀寫逾時，避免 Handshake 過程無回應卡死
                _tcp.ReceiveTimeout = 10000;
                _tcp.SendTimeout = 10000;

                _stream = _tcp.GetStream();

                Handshake();

                _state = 1;
                SafeInvoke(() => Connected?.Invoke(this, EventArgs.Empty));

                ReceiveLoop();

                if (!_intentionalDisconnect)
                    SafeInvoke(() => Disconnected?.Invoke(this, new DisconnectedEventArgs("伺服器已中斷連線。")));
            }
            catch (Exception ex)
            {
                if (!_intentionalDisconnect)
                    SafeInvoke(() => Disconnected?.Invoke(this, new DisconnectedEventArgs(ex.Message)));
            }
            finally
            {
                _state = 0;
                try { _tcp?.Close(); } catch { }
            }
        }

        // ------------------------------------------------------------------ RFB 協定握手

        private void Handshake()
        {
            // 1. 版本協商
            byte[] serverVerBuf = new byte[12];
            ReadFull(serverVerBuf);
            string serverVer = Encoding.ASCII.GetString(serverVerBuf);

            bool rfb38 = serverVer.StartsWith("RFB 003.008") || serverVer.StartsWith("RFB 003.009");
            string clientVer = rfb38 ? "RFB 003.008\n" : "RFB 003.003\n";
            _stream.Write(Encoding.ASCII.GetBytes(clientVer), 0, 12);

            // 2. 安全性認證
            if (rfb38)
            {
                int typeCount = _stream.ReadByte();
                if (typeCount <= 0)
                {
                    uint reasonLen = ReadUInt32BE();
                    byte[] reasonBytes = new byte[reasonLen];
                    ReadFull(reasonBytes);
                    throw new InvalidOperationException("伺服器拒絕連線：" + Encoding.UTF8.GetString(reasonBytes));
                }

                byte[] types = new byte[typeCount];
                ReadFull(types);

                byte chosen = 1; // Default: None
                if (_host.VncAuthType == VncAuthType.MsLogon)
                {
                    if (Array.IndexOf(types, (byte)0x71) >= 0) chosen = 0x71;
                    else if (Array.IndexOf(types, (byte)17) >= 0) chosen = 17;
                    else if (Array.IndexOf(types, (byte)2) >= 0) chosen = 2;
                }
                else if (Array.IndexOf(types, (byte)2) >= 0)
                {
                    chosen = 2;
                }

                _stream.WriteByte(chosen);

                if (chosen == 0x71 || chosen == 17)
                {
                    AuthMsLogon(_host.Username, _host.Domain, _host.Password);
                    if (ReadUInt32BE() != 0) throw new InvalidOperationException("MS-Logon 認證失敗。");
                }
                else if (chosen == 2)
                {
                    AuthVncPassword(_host.Password);
                    if (ReadUInt32BE() != 0) throw new InvalidOperationException("VNC 密碼認證失敗。");
                }
            }
            else
            {
                uint secType = ReadUInt32BE();
                if (secType == 2)
                {
                    AuthVncPassword(_host.Password);
                    if (ReadUInt32BE() != 0) throw new InvalidOperationException("VNC 認證失敗。");
                }
                else if (secType == 0xfffffffa)
                {
                    AuthMsLogon(_host.Username, _host.Domain, _host.Password);
                    if (ReadUInt32BE() != 0) throw new InvalidOperationException("MS-Logon 認證失敗。");
                }
            }

            // 3. ClientInit (Shared=1)
            _stream.WriteByte(1);

            // 4. ServerInit
            _fbW = ReadUInt16BE();
            _fbH = ReadUInt16BE();
            ReadFull(new byte[16]); // Skip PixelFormat
            uint nameLen = ReadUInt32BE();
            ReadFull(new byte[nameLen]);

            lock (_fbLock) _framebuffer = new Bitmap(_fbW, _fbH, PixelFormat.Format32bppRgb);

            // 5. SetPixelFormat (32-bpp RGB)
            byte[] spf = new byte[20];
            spf[0] = 0; spf[4] = 32; spf[5] = 24; spf[7] = 1;
            spf[9] = 255; spf[11] = 255; spf[13] = 255; spf[14] = 16; spf[15] = 8;
            _stream.Write(spf, 0, 20);

            // 6. SetEncodings (Raw + DesktopSize)
            byte[] se = new byte[12];
            se[0] = 2; se[3] = 2; // Count=2
            se[11] = 0x21; // DesktopSize (-223)
            _stream.Write(se, 0, 12);

            SendFbUpdateRequest(false);
        }

        // ------------------------------------------------------------------ 接收迴圈

        private void ReceiveLoop()
        {
            while (true)
            {
                int msgType = _stream.ReadByte();
                if (msgType < 0) break;

                switch (msgType)
                {
                    case 0: HandleFramebufferUpdate(); break;
                    case 1: SkipBytes(ReadUInt16BE() * 6 + 3); break; // ColourMap
                    case 3: SkipBytes((int)ReadUInt32BE() + 3); break; // ServerCutText
                    case 4: _stream.ReadByte(); ResizeFramebuffer(ReadUInt16BE(), ReadUInt16BE()); SafeInvoke(Invalidate); break;
                    default: /* 其他 UltraVNC 訊息略過 */ break;
                }
            }
        }

        private void HandleFramebufferUpdate()
        {
            _stream.ReadByte(); // Padding
            int rectCount = ReadUInt16BE();
            for (int i = 0; i < rectCount; i++)
            {
                int x = ReadUInt16BE(); int y = ReadUInt16BE();
                int w = ReadUInt16BE(); int h = ReadUInt16BE();
                int encoding = (int)ReadUInt32BE();

                if (encoding == 0) ApplyRawRect(x, y, w, h);
                else if (encoding == -223) ResizeFramebuffer(w, h);
            }
            SafeInvoke(Invalidate);
            SendFbUpdateRequest(true);
        }

        private void ApplyRawRect(int x, int y, int w, int h)
        {
            byte[] buf = new byte[w * h * 4];
            ReadFull(buf);
            lock (_fbLock)
            {
                if (_framebuffer == null || x + w > _fbW || y + h > _fbH) return;
                var bmpData = _framebuffer.LockBits(new Rectangle(x, y, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                try {
                    for (int row = 0; row < h; row++)
                        System.Runtime.InteropServices.Marshal.Copy(buf, row * w * 4, new IntPtr(bmpData.Scan0.ToInt64() + row * bmpData.Stride), w * 4);
                } finally { _framebuffer.UnlockBits(bmpData); }
            }
        }

        private void ResizeFramebuffer(int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            lock (_fbLock) {
                _fbW = w; _fbH = h;
                var old = _framebuffer;
                _framebuffer = new Bitmap(w, h, PixelFormat.Format32bppRgb);
                old?.Dispose();
            }
        }

        // ------------------------------------------------------------------ 認證邏輯 (MS-Logon II ECB 修正)

        private void AuthMsLogon(string user, string domain, string pass)
        {
            ulong g = ReadUInt64BE(); ulong p = ReadUInt64BE(); ulong serverPub = ReadUInt64BE();
            byte[] rnd = new byte[8]; new RNGCryptoServiceProvider().GetBytes(rnd);
            ulong clientPriv = (BitConverter.ToUInt64(rnd, 0) % ((1UL << 31) - 1)) + 1;

            WriteUInt64BE(ModPow(g, clientPriv, p));
            ulong shared = ModPow(serverPub, clientPriv, p);

            byte[] key = new byte[8];
            for (int i = 0; i < 8; i++) key[i] = (byte)(shared >> (56 - 8 * i));
            
            // 防止 .NET 弱金鑰例外
            if (DESCryptoServiceProvider.IsWeakKey(key) || DESCryptoServiceProvider.IsSemiWeakKey(key)) key[0] ^= 0x01;

            string login = string.IsNullOrEmpty(domain) ? user : domain + "\\" + user;
            _stream.Write(VncEncryptECB(key, PadToSize(login, 256)), 0, 256);
            _stream.Write(VncEncryptECB(key, PadToSize(pass, 64)), 0, 64);
        }

        private static byte[] VncEncryptECB(byte[] key, byte[] data)
        {
            using (var des = new DESCryptoServiceProvider { Key = key, Mode = CipherMode.ECB, Padding = PaddingMode.None })
            using (var enc = des.CreateEncryptor())
                return enc.TransformFinalBlock(data, 0, data.Length);
        }

        private void AuthVncPassword(string pass)
        {
            byte[] challenge = new byte[16]; ReadFull(challenge);
            byte[] key = new byte[8];
            byte[] pwBytes = Encoding.ASCII.GetBytes(pass ?? "");
            for (int i = 0; i < 8; i++) key[i] = ReverseBits(i < pwBytes.Length ? pwBytes[i] : (byte)0);

            using (var des = new DESCryptoServiceProvider { Key = key, Mode = CipherMode.ECB, Padding = PaddingMode.None })
            using (var enc = des.CreateEncryptor()) {
                byte[] resp = new byte[16];
                enc.TransformBlock(challenge, 0, 16, resp, 0);
                _stream.Write(resp, 0, 16);
            }
        }

        private static byte ReverseBits(byte b) {
            byte r = 0; for (int i = 0; i < 8; i++) r |= (byte)(((b >> i) & 1) << (7 - i)); return r;
        }

        private static ulong ModPow(ulong b, ulong e, ulong m) => (ulong)BigInteger.ModPow(new BigInteger(b), new BigInteger(e), new BigInteger(m));

        private static byte[] PadToSize(string s, int size) {
            byte[] b = new byte[size]; byte[] r = Encoding.ASCII.GetBytes(s ?? "");
            Array.Copy(r, b, Math.Min(r.Length, size - 1)); return b;
        }

        // ------------------------------------------------------------------ 輔助工具

        private void SafeInvoke(Action a) { try { if (!IsDisposed && IsHandleCreated) BeginInvoke(a); } catch { } }

        private void ReadFull(byte[] b) {
            int o = 0; while (o < b.Length) {
                int n = _stream.Read(b, o, b.Length - o);
                if (n <= 0) throw new System.IO.IOException("連線已中斷。"); o += n;
            }
        }

        private void SkipBytes(int n) { if (n > 0) ReadFull(new byte[n]); }
        private int ReadUInt16BE() { byte[] b = new byte[2]; ReadFull(b); return (b[0] << 8) | b[1]; }
        private uint ReadUInt32BE() { byte[] b = new byte[4]; ReadFull(b); return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]); }
        private ulong ReadUInt64BE() { byte[] b = new byte[8]; ReadFull(b); ulong v = 0; for(int i=0; i<8; i++) v = (v << 8) | b[i]; return v; }
        private void WriteUInt64BE(ulong v) { byte[] b = new byte[8]; for(int i=7; i>=0; i--) { b[i] = (byte)v; v >>= 8; } _stream.Write(b, 0, 8); }

        private void SendFbUpdateRequest(bool inc) {
            byte[] r = new byte[10]; r[0] = 3; r[1] = (byte)(inc ? 1 : 0);
            r[6] = (byte)(_fbW >> 8); r[7] = (byte)_fbW; r[8] = (byte)(_fbH >> 8); r[9] = (byte)_fbH;
            try { _stream?.Write(r, 0, 10); } catch { }
        }

        // ------------------------------------------------------------------ UI 與 輸入事件

        public VncClientControl() { 
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true); 
        }

        protected override void OnPaint(PaintEventArgs e) {
            lock (_fbLock) { if (_framebuffer != null) e.Graphics.DrawImage(_framebuffer, ClientRectangle); else e.Graphics.Clear(Color.Black); }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e); if (_state == 1) SendPointerEvent(e, GetButtonMask(Control.MouseButtons));
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e); if (_state == 1) SendPointerEvent(e, GetButtonMask(Control.MouseButtons));
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e); if (_state == 1) SendPointerEvent(e, GetButtonMask(Control.MouseButtons));
        }

        private void SendPointerEvent(MouseEventArgs e, byte mask) {
            int x = _fbW > 0 ? e.X * _fbW / Math.Max(Width, 1) : 0;
            int y = _fbH > 0 ? e.Y * _fbH / Math.Max(Height, 1) : 0;
            byte[] m = { 5, mask, (byte)(x >> 8), (byte)x, (byte)(y >> 8), (byte)y };
            try { _stream?.WriteAsync(m, 0, 6); } catch { }
        }

        private static byte GetButtonMask(MouseButtons b) {
            byte m = 0; if ((b & MouseButtons.Left) != 0) m |= 1;
            if ((b & MouseButtons.Middle) != 0) m |= 2; if ((b & MouseButtons.Right) != 0) m |= 4; return m;
        }

        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); SendKey(e.KeyCode, true); }
        protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); SendKey(e.KeyCode, false); }

        private void SendKey(Keys k, bool down) {
            if (_state != 1 || !KeyMap.TryGetValue(k, out uint sym)) return;
            byte[] m = { 4, (byte)(down ? 1 : 0), 0, 0, (byte)(sym >> 24), (byte)(sym >> 16), (byte)(sym >> 8), (byte)sym };
            try { _stream?.WriteAsync(m, 0, 8); } catch { }
        }

        private static readonly Dictionary<Keys, uint> KeyMap = new Dictionary<Keys, uint> {
            { Keys.A, 0x61 }, { Keys.B, 0x62 }, { Keys.C, 0x63 }, { Keys.Return, 0xFF0D }, { Keys.Escape, 0xFF1B },
            { Keys.Back, 0xFF08 }, { Keys.Tab, 0xFF09 }, { Keys.Left, 0xFF51 }, { Keys.Up, 0xFF52 }, { Keys.Right, 0xFF53 }, { Keys.Down, 0xFF54 }
            /* ... 可根據需求補完剩餘 KeyMap ... */
        };

        protected override void Dispose(bool disposing) {
            if (disposing) { _intentionalDisconnect = true; try { _tcp?.Close(); } catch { } lock (_fbLock) { _framebuffer?.Dispose(); _framebuffer = null; } }
            base.Dispose(disposing);
        }
    }
}
