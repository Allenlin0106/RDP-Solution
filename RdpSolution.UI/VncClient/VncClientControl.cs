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
    /// Pure-.NET RFB 3.3/3.8 client implementing VNC Password (type 2) and
    /// UltraVNC MS-Logon II (type 17) authentication.
    /// </summary>
    public sealed class VncClientControl : UserControl, IRemoteControl
    {
        // ------------------------------------------------------------------ state

        private TcpClient     _tcp;
        private NetworkStream _stream;
        private Thread        _receiveThread;

        private Bitmap _framebuffer;
        private readonly object _fbLock = new object();
        private int _fbW, _fbH;

        private volatile int _state;   // 0=off 1=connected 2=connecting
        private bool _intentionalDisconnect;

        private RemoteHostConfig _host;

        // ------------------------------------------------------------------ IRemoteControl

        public int ConnectedState => _state;

        public event EventHandler Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        // Never fires — pure .NET control always creates successfully.
        public event EventHandler<CreateFailedEventArgs> CreateFailed;

        // ------------------------------------------------------------------ IRemoteControl actions

        public void Connect(RemoteHostConfig host)
        {
            _host                  = host;
            _intentionalDisconnect = false;
            _state                 = 2;

            // Accessing Handle forces the window handle to be created synchronously
            // on the calling (UI) thread.  Without this, the background thread can
            // complete the handshake before WM_CREATE is processed, causing
            // IsHandleCreated to be false when BeginInvoke is called, which silently
            // drops the Connected / Disconnected events and leaves the UI frozen in
            // "Connecting…" with a permanently black framebuffer.
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

        // ------------------------------------------------------------------ background thread

        private void ConnectAndReceive()
        {
            try
            {
                _tcp = new TcpClient();
                
                // 1. 設定網路讀寫逾時，避免 Handshake 時伺服器無回應導致卡死
                _tcp.ReceiveTimeout = 10000;
                _tcp.SendTimeout = 10000;

                // 2. 使用非同步等待來限制連線最大等待時間 (10秒)
                var connectResult = _tcp.BeginConnect(_host.Hostname, _host.Port, null, null);
                bool success = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                
                if (!success)
                {
                    throw new TimeoutException("連線逾時：請確認 IP、Port 是否正確，或檢查防火牆設定。");
                }
                _tcp.EndConnect(connectResult);

                _stream = _tcp.GetStream();

                Handshake();

                _state = 1;
                SafeInvoke(() => Connected?.Invoke(this, EventArgs.Empty));

                ReceiveLoop();

                // Normal EOF — server closed the connection
                if (!_intentionalDisconnect)
                    SafeInvoke(() => Disconnected?.Invoke(this,
                        new DisconnectedEventArgs("Server closed the connection.")));
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

        // ------------------------------------------------------------------ RFB handshake

        private void Handshake()
        {
            // 1. Version negotiation
            byte[] serverVerBuf = new byte[12];
            ReadFull(serverVerBuf);
            string serverVer = Encoding.ASCII.GetString(serverVerBuf);

            bool rfb38 = serverVer.StartsWith("RFB 003.008") ||
                         serverVer.StartsWith("RFB 003.009");
            string clientVer = rfb38 ? "RFB 003.008\n" : "RFB 003.003\n";
            _stream.Write(Encoding.ASCII.GetBytes(clientVer), 0, 12);

            // 2. Security
            if (rfb38)
            {
                int typeCount = _stream.ReadByte();
                if (typeCount == 0)
                {
                    uint reasonLen = ReadUInt32BE();
                    byte[] reasonBytes = new byte[reasonLen];
                    ReadFull(reasonBytes);
                    throw new InvalidOperationException(
                        "Server rejected connection: " + Encoding.UTF8.GetString(reasonBytes));
                }

                byte[] types = new byte[typeCount];
                ReadFull(types);

                bool hasType113 = Array.IndexOf(types, (byte)0x71) >= 0; // rfbUltraVNC_MsLogonIIAuth (direct)
                bool hasType17  = Array.IndexOf(types, (byte)17)   >= 0; // rfbUltraVNC outer (older servers)
                bool hasType2   = Array.IndexOf(types, (byte)2)    >= 0; // VNC Password

                byte chosen;
                if (_host.VncAuthType == VncAuthType.MsLogon)
                {
                    if (hasType113)    chosen = 0x71;
                    else if (hasType17) chosen = 17;
                    else if (hasType2)  chosen = 2;
                    else               chosen = 1;
                }
                else
                {
                    if (hasType2)      chosen = 2;
                    else               chosen = 1;
                }

                _stream.WriteByte(chosen);

                if (chosen == 0x71 || chosen == 17)
                {
                    AuthMsLogon(
                        _host.Username ?? string.Empty,
                        _host.Domain   ?? string.Empty,
                        _host.Password ?? string.Empty);

                    uint authResult = ReadUInt32BE();
                    if (authResult != 0)
                        throw new InvalidOperationException(
                            "UltraVNC MS-Logon II authentication failed (result " + authResult + ").");
                }
                else if (chosen == 2)
                {
                    AuthVncPassword(_host.Password ?? string.Empty);
                    uint result = ReadUInt32BE();
                    if (result != 0)
                    {
                        string reason = "VNC authentication failed.";
                        try
                        {
                            uint reasonLen = ReadUInt32BE();
                            if (reasonLen > 0 && reasonLen <= 4096)
                            {
                                byte[] rb = new byte[reasonLen];
                                ReadFull(rb);
                                reason = "VNC authentication failed: " + Encoding.UTF8.GetString(rb);
                            }
                        }
                        catch { /* server closed without reason string */ }
                        throw new InvalidOperationException(reason);
                    }
                }
            }
            else
            {
                uint secType = ReadUInt32BE();
                if (secType == 0)
                {
                    throw new InvalidOperationException("Server sent security failure.");
                }
                else if (secType == 2)
                {
                    AuthVncPassword(_host.Password ?? string.Empty);
                    uint result = ReadUInt32BE();
                    if (result != 0)
                        throw new InvalidOperationException("VNC authentication failed.");
                }
                else if (secType == 0xfffffffa) // Legacy UltraVNC MS-Logon II (pre-3.8)
                {
                    AuthMsLogon(
                        _host.Username ?? string.Empty,
                        _host.Domain   ?? string.Empty,
                        _host.Password ?? string.Empty);
                    uint result = ReadUInt32BE();
                    if (result != 0)
                        throw new InvalidOperationException("UltraVNC MS-Logon II authentication failed.");
                }
            }

            // 3. ClientInit
            _stream.WriteByte(1);

            // 4. ServerInit
            _fbW = ReadUInt16BE();
            _fbH = ReadUInt16BE();

            byte[] pixFmt = new byte[16];
            ReadFull(pixFmt);

            uint nameLen = ReadUInt32BE();
            byte[] nameBytes = new byte[nameLen];
            ReadFull(nameBytes);

            lock (_fbLock)
                _framebuffer = new Bitmap(_fbW, _fbH, PixelFormat.Format32bppRgb);

            // 5. SetPixelFormat
            byte[] spf = new byte[20];
            spf[0] = 0;    
            spf[4] = 32;   
            spf[5] = 24;   
            spf[6] = 0;    
            spf[7] = 1;    
            spf[8] = 0; spf[9] = 255;
            spf[10] = 0; spf[11] = 255;
            spf[12] = 0; spf[13] = 255;
            spf[14] = 16;  
            spf[15] = 8;   
            spf[16] = 0;   
            _stream.Write(spf, 0, 20);

            // 6. SetEncodings
            byte[] se = new byte[12];
            se[0] = 2;                                   
            se[2] = 0; se[3] = 2;                        
            se[4] = 0; se[5] = 0; se[6] = 0; se[7] = 0;
            se[8] = 0xFF; se[9] = 0xFF; se[10] = 0xFF; se[11] = 0x21;
            _stream.Write(se, 0, 12);

            // 7. Initial FramebufferUpdateRequest
            SendFbUpdateRequest(incremental: false);
        }

        // ------------------------------------------------------------------ receive loop

        private void ReceiveLoop()
        {
            while (true)
            {
                int msgType = _stream.ReadByte();
                if (msgType < 0) break;

                switch (msgType)
                {
                    case 0:    HandleFramebufferUpdate();            break;
                    case 1:    SkipColourMapEntries();               break;
                    case 2:    /* Bell — no-op */                    break;
                    case 3:    SkipServerCutText();                  break;
                    case 4:    HandleUltraVncResizeFrameBuffer();    break;
                    case 7:    SkipUltraVncFileTransfer();           break;
                    case 8:    ReadFull(new byte[3]);                break;
                    case 9:    ReadFull(new byte[3]);                break;
                    case 10:   ReadFull(new byte[5]);                break;
                    case 11:   SkipUltraVncTextChat();               break;
                    case 13:   /* rfbKeepAlive — no payload */       break;
                    case 15:   ReadFull(new byte[11]);               break;
                    case 0xAD: ReadFull(new byte[11]);               break;
                    default:
                        throw new System.IO.IOException(
                            "Unsupported server message type " + msgType +
                            ". Stream is now corrupt; disconnecting.");
                }
            }
        }

        private void HandleUltraVncResizeFrameBuffer()
        {
            _stream.ReadByte();
            int w = ReadUInt16BE();
            int h = ReadUInt16BE();
            ResizeFramebuffer(w, h);
            SafeInvoke(Invalidate);
        }

        private void SkipUltraVncFileTransfer()
        {
            ReadFull(new byte[7]); 
            uint length = ReadUInt32BE();
            if (length > 64 * 1024 * 1024)
                throw new System.IO.IOException("rfbFileTransfer payload too large: " + length + " bytes.");
            if (length > 0)
                ReadFull(new byte[length]);
        }

        private void SkipUltraVncTextChat()
        {
            ReadFull(new byte[3]); 
            uint length = ReadUInt32BE();
            if (length > 64 * 1024)
                throw new System.IO.IOException("rfbTextChat payload too large: " + length + " bytes.");
            if (length > 0)
                ReadFull(new byte[length]);
        }

        private void HandleFramebufferUpdate()
        {
            _stream.ReadByte(); 
            int rectCount = ReadUInt16BE();

            for (int i = 0; i < rectCount; i++)
            {
                int x        = ReadUInt16BE();
                int y        = ReadUInt16BE();
                int w        = ReadUInt16BE();
                int h        = ReadUInt16BE();
                int encoding = (int)ReadUInt32BE();

                switch (encoding)
                {
                    case 0:
                        if (w > 0 && h > 0)
                            ApplyRawRect(x, y, w, h);
                        break;
                    case -223:
                        ResizeFramebuffer(w, h);
                        break;
                    case -239:
                        if (w > 0 && h > 0)
                        {
                            ReadFull(new byte[w * h * 4]);             
                            ReadFull(new byte[((w + 7) / 8) * h]);     
                        }
                        break;
                    case -240:
                        if (w > 0 && h > 0)
                        {
                            ReadFull(new byte[6]);                          
                            ReadFull(new byte[((w + 7) / 8) * h * 2]);     
                        }
                        break;
                    case 1:
                        ReadFull(new byte[4]);
                        break;
                    default:
                        throw new System.IO.IOException(
                            "Unsupported rectangle encoding " + encoding +
                            ". Stream is now corrupt; disconnecting.");
                }
            }

            SafeInvoke(Invalidate);
            SendFbUpdateRequest(incremental: true);
        }

        private void ApplyRawRect(int x, int y, int w, int h)
        {
            byte[] buf = new byte[w * h * 4];
            ReadFull(buf);

            lock (_fbLock)
            {
                if (_framebuffer == null || x + w > _fbW || y + h > _fbH) return;

                var rect    = new Rectangle(x, y, w, h);
                var bmpData = _framebuffer.LockBits(rect,
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                try
                {
                    int rowBytes = w * 4;
                    IntPtr scan0 = bmpData.Scan0;
                    for (int row = 0; row < h; row++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(
                            buf, row * rowBytes,
                            new IntPtr(scan0.ToInt64() + (long)row * bmpData.Stride),
                            rowBytes);
                    }
                }
                finally { _framebuffer.UnlockBits(bmpData); }
            }
        }

        private void ResizeFramebuffer(int newW, int newH)
        {
            if (newW <= 0 || newH <= 0) return;
            lock (_fbLock)
            {
                _fbW = newW;
                _fbH = newH;
                var old = _framebuffer;
                _framebuffer = new Bitmap(newW, newH, PixelFormat.Format32bppRgb);
                old?.Dispose();
            }
        }

        private void SkipColourMapEntries()
        {
            _stream.ReadByte(); 
            ReadUInt16BE();     
            int colourCount = ReadUInt16BE();
            byte[] skip = new byte[colourCount * 6];
            ReadFull(skip);
        }

        private void SkipServerCutText()
        {
            byte[] pad = new byte[3];
            ReadFull(pad);
            uint length = ReadUInt32BE();
            if (length > 0)
            {
                byte[] text = new byte[length];
                ReadFull(text);
            }
        }

        private void SendFbUpdateRequest(bool incremental)
        {
            byte[] req = new byte[10];
            req[0] = 3;                                      
            req[1] = (byte)(incremental ? 1 : 0);
            req[6] = (byte)(_fbW >> 8); req[7] = (byte)_fbW;
            req[8] = (byte)(_fbH >> 8); req[9] = (byte)_fbH;
            _stream.Write(req, 0, 10);
        }

        // ------------------------------------------------------------------ VNC Auth (type 2)

        private void AuthVncPassword(string password)
        {
            byte[] challenge = new byte[16];
            ReadFull(challenge);

            byte[] keyBytes = new byte[8];
            byte[] pwBytes  = Encoding.ASCII.GetBytes(password);
            for (int i = 0; i < 8; i++)
                keyBytes[i] = i < pwBytes.Length ? ReverseBits(pwBytes[i]) : (byte)0;

            byte[] response = new byte[16];
            using (var des = new DESCryptoServiceProvider())
            {
                des.Key     = keyBytes;
                des.Mode    = CipherMode.ECB;
                des.Padding = PaddingMode.None;
                using (var enc = des.CreateEncryptor())
                {
                    enc.TransformBlock(challenge, 0, 8, response, 0);
                    enc.TransformBlock(challenge, 8, 8, response, 8);
                }
            }

            _stream.Write(response, 0, 16);
        }

        private static byte ReverseBits(byte b)
        {
            byte r = 0;
            for (int i = 0; i < 8; i++) r |= (byte)(((b >> i) & 1) << (7 - i));
            return r;
        }

        // ------------------------------------------------------------------ MS-Logon II (type 17)

        private void AuthMsLogon(string username, string domain, string password)
        {
            ulong g         = ReadUInt64BE();
            ulong p         = ReadUInt64BE();
            ulong serverPub = ReadUInt64BE();

            byte[] rnd = new byte[8];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(rnd);
            ulong clientPriv = (BitConverter.ToUInt64(rnd, 0) % ((1UL << 31) - 1)) + 1;

            ulong clientPub = ModPow(g, clientPriv, p);
            ulong shared    = ModPow(serverPub, clientPriv, p);

            WriteUInt64BE(clientPub);

            // 取得 8 bytes 的 DH Shared Secret
            byte[] dhSecret = new byte[8];
            for (int i = 0; i < 8; i++)
                dhSecret[i] = (byte)(shared >> (56 - 8 * i));

            // 修正核心：DES Key 必須經過位元反轉 (Bit-Reversal)
            byte[] desKeyReversed = new byte[8];
            for (int i = 0; i < 8; i++)
                desKeyReversed[i] = ReverseBits(dhSecret[i]);

            // 修正核心：初始向量 (IV) 必須使用「反轉前」的原始 dhSecret
            byte[] iv = (byte[])dhSecret.Clone();

            string userField = string.IsNullOrEmpty(domain)
                ? username
                : domain + "\\" + username;

            // 傳入 desKeyReversed 與原始的 iv 進行加密
            byte[] encUser = VncEncryptBytes2(desKeyReversed, ref iv, PadToSize(userField, 256));
            byte[] encPass = VncEncryptBytes2(desKeyReversed, ref iv, PadToSize(password,  64));

            _stream.Write(encUser, 0, 256);
            _stream.Write(encPass, 0,  64);
        }

        private static ulong ModPow(ulong b, ulong e, ulong m)
        {
            if (m == 1) return 0;
            return (ulong)BigInteger.ModPow(
                new BigInteger(b), new BigInteger(e), new BigInteger(m));
        }

        private static byte[] PadToSize(string s, int size)
        {
            byte[] buf = new byte[size];
            // 修正編碼：使用 ASCII 避免多位元組導致伺服器端解密字串錯亂
            byte[] raw = Encoding.ASCII.GetBytes(s ?? string.Empty);
            Array.Copy(raw, buf, Math.Min(raw.Length, size - 1));
            return buf;
        }

        private static byte[] VncEncryptBytes2(byte[] key8, ref byte[] iv, byte[] plaintext)
        {
            byte[] buf   = (byte[])plaintext.Clone();
            byte[] block = new byte[8];
            using (var des = new DESCryptoServiceProvider())
            {
                des.Key     = key8;
                des.Mode    = CipherMode.ECB;
                des.Padding = PaddingMode.None;
                using (var enc = des.CreateEncryptor())
                {
                    for (int i = 0; i < buf.Length; i += 8)
                    {
                        for (int j = 0; j < 8; j++) buf[i + j] ^= iv[j];
                        enc.TransformBlock(buf, i, 8, block, 0);
                        Array.Copy(block, 0, buf, i, 8);
                        Array.Copy(block, 0, iv,  0, 8); 
                    }
                }
            }
            return buf;
        }

        // ------------------------------------------------------------------ UI invoke helper

        private void SafeInvoke(Action action)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                
                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch { }
        }

        // ------------------------------------------------------------------ stream helpers

        private void ReadFull(byte[] buf)
        {
            int offset = 0;
            while (offset < buf.Length)
            {
                int n = _stream.Read(buf, offset, buf.Length - offset);
                if (n == 0) throw new System.IO.IOException("Connection closed.");
                offset += n;
            }
        }

        private int ReadUInt16BE()
        {
            byte[] b = new byte[2];
            ReadFull(b);
            return (b[0] << 8) | b[1];
        }

        private uint ReadUInt32BE()
        {
            byte[] b = new byte[4];
            ReadFull(b);
            return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        }

        private ulong ReadUInt64BE()
        {
            byte[] b = new byte[8];
            ReadFull(b);
            return ((ulong)b[0] << 56) | ((ulong)b[1] << 48) | ((ulong)b[2] << 40) |
                   ((ulong)b[3] << 32) | ((ulong)b[4] << 24) | ((ulong)b[5] << 16) |
                   ((ulong)b[6] <<  8) | b[7];
        }

        private void WriteUInt64BE(ulong v)
        {
            byte[] b = new byte[]
            {
                (byte)(v >> 56), (byte)(v >> 48), (byte)(v >> 40), (byte)(v >> 32),
                (byte)(v >> 24), (byte)(v >> 16), (byte)(v >>  8), (byte)v
            };
            _stream.Write(b, 0, 8);
        }

        // ------------------------------------------------------------------ rendering

        public VncClientControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            lock (_fbLock)
            {
                if (_framebuffer != null)
                    e.Graphics.DrawImage(_framebuffer, ClientRectangle);
                else
                    e.Graphics.Clear(Color.Black);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_state == 1) SendPointerEvent(e, GetButtonMask(Control.MouseButtons));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_state == 1) SendPointerEvent(e, GetButtonMask(e.Button));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_state == 1) SendPointerEvent(e, 0);
        }

        private void SendPointerEvent(MouseEventArgs e, byte buttonMask)
        {
            int x = _fbW  > 0 ? e.X * _fbW  / Math.Max(Width,  1) : 0;
            int y = _fbH  > 0 ? e.Y * _fbH  / Math.Max(Height, 1) : 0;

            byte[] msg = new byte[6];
            msg[0] = 5;
            msg[1] = buttonMask;
            msg[2] = (byte)(x >> 8); msg[3] = (byte)x;
            msg[4] = (byte)(y >> 8); msg[5] = (byte)y;
            try { _stream?.WriteAsync(msg, 0, 6); } catch { }
        }

        private static byte GetButtonMask(MouseButtons b)
        {
            byte m = 0;
            if ((b & MouseButtons.Left)   != 0) m |= 1;
            if ((b & MouseButtons.Middle) != 0) m |= 2;
            if ((b & MouseButtons.Right)  != 0) m |= 4;
            return m;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_state == 1) SendKeyEvent(e.KeyCode, down: true);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (_state == 1) SendKeyEvent(e.KeyCode, down: false);
        }

        private void SendKeyEvent(Keys key, bool down)
        {
            uint keysym;
            if (!KeyMap.TryGetValue(key, out keysym)) return;

            byte[] msg = new byte[8];
            msg[0] = 4;
            msg[1] = (byte)(down ? 1 : 0);
            msg[4] = (byte)(keysym >> 24);
            msg[5] = (byte)(keysym >> 16);
            msg[6] = (byte)(keysym >>  8);
            msg[7] = (byte) keysym;
            try { _stream?.WriteAsync(msg, 0, 8); } catch { }
        }

        // ------------------------------------------------------------------ key map

        private static readonly Dictionary<Keys, uint> KeyMap = new Dictionary<Keys, uint>
        {
            { Keys.A, 0x61 }, { Keys.B, 0x62 }, { Keys.C, 0x63 }, { Keys.D, 0x64 },
            { Keys.E, 0x65 }, { Keys.F, 0x66 }, { Keys.G, 0x67 }, { Keys.H, 0x68 },
            { Keys.I, 0x69 }, { Keys.J, 0x6A }, { Keys.K, 0x6B }, { Keys.L, 0x6C },
            { Keys.M, 0x6D }, { Keys.N, 0x6E }, { Keys.O, 0x6F }, { Keys.P, 0x70 },
            { Keys.Q, 0x71 }, { Keys.R, 0x72 }, { Keys.S, 0x73 }, { Keys.T, 0x74 },
            { Keys.U, 0x75 }, { Keys.V, 0x76 }, { Keys.W, 0x77 }, { Keys.X, 0x78 },
            { Keys.Y, 0x79 }, { Keys.Z, 0x7A },
            { Keys.D0, 0x30 }, { Keys.D1, 0x31 }, { Keys.D2, 0x32 }, { Keys.D3, 0x33 },
            { Keys.D4, 0x34 }, { Keys.D5, 0x35 }, { Keys.D6, 0x36 }, { Keys.D7, 0x37 },
            { Keys.D8, 0x38 }, { Keys.D9, 0x39 },
            { Keys.Return,  0xFF0D }, { Keys.Escape,   0xFF1B },
            { Keys.Back,    0xFF08 }, { Keys.Tab,      0xFF09 },
            { Keys.ShiftKey,  0xFFE1 }, { Keys.RShiftKey, 0xFFE2 },
            { Keys.ControlKey, 0xFFE3 }, { Keys.RControlKey, 0xFFE4 },
            { Keys.Menu,    0xFFE9 }, { Keys.RMenu,   0xFFEA },
            { Keys.F1,  0xFFBE }, { Keys.F2,  0xFFBF }, { Keys.F3,  0xFFC0 },
            { Keys.F4,  0xFFC1 }, { Keys.F5,  0xFFC2 }, { Keys.F6,  0xFFC3 },
            { Keys.F7,  0xFFC4 }, { Keys.F8,  0xFFC5 }, { Keys.F9,  0xFFC6 },
            { Keys.F10, 0xFFC7 }, { Keys.F11, 0xFFC8 }, { Keys.F12, 0xFFC9 },
            { Keys.Left,   0xFF51 }, { Keys.Up,     0xFF52 },
            { Keys.Right,  0xFF53 }, { Keys.Down,   0xFF54 },
            { Keys.Home,   0xFF50 }, { Keys.End,    0xFF57 },
            { Keys.Prior,  0xFF55 }, { Keys.Next,   0xFF56 },
            { Keys.Insert, 0xFF63 }, { Keys.Delete, 0xFFFF },
        };

        // ------------------------------------------------------------------ dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _intentionalDisconnect = true;
                try { _tcp?.Close(); } catch { }
                lock (_fbLock) { _framebuffer?.Dispose(); _framebuffer = null; }
            }
            base.Dispose(disposing);
        }
    }
}
