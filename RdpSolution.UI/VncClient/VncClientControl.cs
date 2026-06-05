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
    public sealed class VncClientControl : UserControl, IRemoteControl
    {
        // -- state
        private TcpClient _tcp;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private Bitmap _framebuffer;
        private readonly object _fbLock = new object();
        private int _fbW, _fbH;
        private volatile int _state;   // 0=off 1=connected 2=connecting
        private bool _intentionalDisconnect;
        private RemoteHostConfig _host;
        private readonly SynchronizationContext _syncContext;

        // -- IRemoteControl
        public int ConnectedState => _state;
        public event EventHandler Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<CreateFailedEventArgs> CreateFailed;

        public VncClientControl()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        // ------------------------------------------------------------------ connect / disconnect

        public void Connect(RemoteHostConfig host)
        {
            _host = host;
            _intentionalDisconnect = false;
            _state = 2;
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
                var ar = _tcp.BeginConnect(_host.Hostname, _host.Port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("連線逾時。");
                _tcp.EndConnect(ar);
                _tcp.ReceiveTimeout = 30000;
                _tcp.SendTimeout    = 10000;
                _stream = _tcp.GetStream();

                Handshake();

                _state = 1;
                Post(() => Connected?.Invoke(this, EventArgs.Empty));

                ReceiveLoop();

                if (!_intentionalDisconnect)
                    Post(() => Disconnected?.Invoke(this, new DisconnectedEventArgs("伺服器已關閉連線。")));
            }
            catch (Exception ex)
            {
                _state = 0;
                if (!_intentionalDisconnect)
                    Post(() => Disconnected?.Invoke(this, new DisconnectedEventArgs(ex.Message)));
            }
            finally
            {
                _state = 0;
                try { _tcp?.Close(); } catch { }
            }
        }

        private void Post(Action a)
        {
            _syncContext.Post(_ => { try { if (!IsDisposed) a(); } catch { } }, null);
        }

        // ------------------------------------------------------------------ RFB handshake

        private void Handshake()
        {
            // 1. Version negotiation
            byte[] sv = new byte[12];
            ReadFull(sv);
            string ver = Encoding.ASCII.GetString(sv);
            bool rfb38 = ver.StartsWith("RFB 003.008") || ver.StartsWith("RFB 003.009");
            _stream.Write(Encoding.ASCII.GetBytes(rfb38 ? "RFB 003.008\n" : "RFB 003.003\n"), 0, 12);

            // 2. Security
            if (rfb38)
            {
                int cnt = _stream.ReadByte();
                if (cnt <= 0)
                {
                    uint rlen = ReadUInt32BE();
                    byte[] rb = new byte[Math.Min(rlen, 4096u)];
                    ReadFull(rb);
                    throw new InvalidOperationException("伺服器拒絕連線：" + Encoding.UTF8.GetString(rb));
                }
                byte[] types = new byte[cnt];
                ReadFull(types);

                byte chosen = SelectSecurityType(types);
                _stream.WriteByte(chosen);

                if (chosen == 0x71 || chosen == 17)
                {
                    AuthMsLogon(_host.Username, _host.Domain, _host.Password);
                    if (ReadUInt32BE() != 0)
                        throw new InvalidOperationException("MS-Logon 認證失敗：" + ReadFailReason());
                }
                else if (chosen == 2)
                {
                    AuthVncPassword(_host.Password);
                    if (ReadUInt32BE() != 0)
                        throw new InvalidOperationException("VNC 密碼認證失敗：" + ReadFailReason());
                }
                // chosen == 1 (None): no SecurityResult
            }
            else // RFB 3.3
            {
                uint secType = ReadUInt32BE();
                if (secType == 0)
                {
                    uint rlen = ReadUInt32BE();
                    byte[] rb = new byte[Math.Min(rlen, 4096u)];
                    ReadFull(rb);
                    throw new InvalidOperationException("伺服器拒絕連線：" + Encoding.UTF8.GetString(rb));
                }
                if (secType == 2)
                {
                    AuthVncPassword(_host.Password);
                    if (ReadUInt32BE() != 0) throw new InvalidOperationException("VNC 密碼認證失敗。");
                }
                else if (secType == 0xfffffffa)
                {
                    AuthMsLogon(_host.Username, _host.Domain, _host.Password);
                    if (ReadUInt32BE() != 0) throw new InvalidOperationException("MS-Logon 認證失敗。");
                }
            }

            // 3. ClientInit (shared=1)
            _stream.WriteByte(1);

            // 4. ServerInit
            _fbW = ReadUInt16BE();
            _fbH = ReadUInt16BE();
            ReadFull(new byte[16]); // pixel format
            uint nameLen = ReadUInt32BE();
            if (nameLen > 0 && nameLen <= 65536) ReadFull(new byte[nameLen]);
            else if (nameLen > 65536) SkipBytes((int)Math.Min(nameLen, 65536u));

            lock (_fbLock)
                _framebuffer = new Bitmap(Math.Max(_fbW, 1), Math.Max(_fbH, 1), PixelFormat.Format32bppRgb);

            // 5. SetPixelFormat: bpp=32 depth=24 bigEndian=0 trueColour=1 R/G/B max=255 shifts R=16 G=8 B=0
            byte[] spf = { 0, 0, 0, 0, 32, 24, 0, 1, 0, 255, 0, 255, 0, 255, 16, 8, 0, 0, 0, 0 };
            _stream.Write(spf, 0, 20);

            // 6. SetEncodings: Raw(0) + DesktopSize(-223 = 0xFFFFFF21)
            byte[] se = { 2, 0, 0, 2,
                          0, 0, 0, 0,              // Raw = 0
                          0xFF, 0xFF, 0xFF, 0x21 }; // DesktopSize = -223
            _stream.Write(se, 0, 12);

            // 7. First FramebufferUpdateRequest (full)
            SendFbUpdateRequest(false);
        }

        private byte SelectSecurityType(byte[] types)
        {
            if (_host.VncAuthType == VncAuthType.MsLogon)
            {
                if (Array.IndexOf(types, (byte)0x71) >= 0) return 0x71;
                if (Array.IndexOf(types, (byte)17)   >= 0) return 17;
            }
            if (Array.IndexOf(types, (byte)2) >= 0) return 2;
            return 1; // None
        }

        private string ReadFailReason()
        {
            try
            {
                uint len = ReadUInt32BE();
                if (len > 0 && len <= 4096)
                {
                    byte[] b = new byte[len];
                    ReadFull(b);
                    return Encoding.UTF8.GetString(b);
                }
            }
            catch { }
            return string.Empty;
        }

        // ------------------------------------------------------------------ receive loop

        private void ReceiveLoop()
        {
            while (true)
            {
                int t = _stream.ReadByte();
                if (t < 0) break;

                switch (t)
                {
                    case 0:  HandleFramebufferUpdate(); break;
                    case 1:  // SetColourMapEntries: pad(1)+first(2)+count(2)+count*6
                        SkipBytes(3);
                        SkipBytes(ReadUInt16BE() * 6);
                        break;
                    case 2:  break; // Bell — no payload
                    case 3:  // ServerCutText: pad(1)+pad(2)+len(4)+text
                        SkipBytes(3);
                        SkipBytes((int)ReadUInt32BE());
                        break;
                    case 4:  // rfbResizeFrameBuffer (UltraVNC): pad(1)+w(2)+h(2)
                        _stream.ReadByte();
                        ResizeFramebuffer(ReadUInt16BE(), ReadUInt16BE());
                        Post(Invalidate);
                        break;
                    case 7:  SkipFileTransfer(); break;
                    case 8:  SkipBytes(3);  break; // rfbSetScale: pad(1)+scale(2)
                    case 9:  SkipBytes(3);  break; // rfbSetServerInput: pad(1)+status(2)
                    case 10: SkipBytes(5);  break; // rfbSetSW: pad(1)+x(2)+y(2)
                    case 11: // rfbTextChat: pad(1)+pad(2)+len(4)+text
                        SkipBytes(3);
                        int chatLen = (int)ReadUInt32BE();
                        if (chatLen > 0 && chatLen < 65536) SkipBytes(chatLen);
                        break;
                    case 13: break; // rfbKeepAlive — no payload
                    case 15: SkipBytes(11); break; // rfbPalmVNCReSizeFrameBuffer
                    case 0xAD: SkipBytes(11); break; // rfbServerState: pad(3)+state(4)+value(4)
                    default:
                        throw new InvalidOperationException("未知伺服器訊息類型 " + t + "，連線中止。");
                }
            }
        }

        private void SkipFileTransfer()
        {
            // rfbFileTransfer: contentType(1)+contentParam(2)+size(4)+length(4)+data
            SkipBytes(7); // contentType + contentParam + size
            int len = (int)ReadUInt32BE();
            if (len > 0 && len < 1024 * 1024 * 64) SkipBytes(len);
        }

        private void HandleFramebufferUpdate()
        {
            _stream.ReadByte(); // padding
            int rectCount = ReadUInt16BE();
            for (int i = 0; i < rectCount; i++)
            {
                int x   = ReadUInt16BE(), y = ReadUInt16BE();
                int w   = ReadUInt16BE(), h = ReadUInt16BE();
                int enc = (int)ReadUInt32BE();

                switch (enc)
                {
                    case 0:    ApplyRawRect(x, y, w, h); break;
                    case -223: ResizeFramebuffer(w, h);  break; // DesktopSize
                    case -239: // RichCursor
                        if (w > 0 && h > 0)
                        {
                            SkipBytes(w * h * 4);
                            SkipBytes(((w + 7) / 8) * h);
                        }
                        break;
                    case -240: // XCursor
                        if (w > 0 && h > 0)
                        {
                            ReadFull(new byte[6]);
                            ReadFull(new byte[((w + 7) / 8) * h * 2]);
                        }
                        break;
                    default: break; // unknown/pseudo encoding — no data to skip for unrecognised
                }
            }
            Post(Invalidate);
            SendFbUpdateRequest(true);
        }

        private void ApplyRawRect(int x, int y, int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            byte[] buf = new byte[w * h * 4];
            ReadFull(buf);
            lock (_fbLock)
            {
                if (_framebuffer == null || x + w > _fbW || y + h > _fbH) return;
                var bd = _framebuffer.LockBits(
                    new Rectangle(x, y, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                try
                {
                    for (int row = 0; row < h; row++)
                        System.Runtime.InteropServices.Marshal.Copy(
                            buf, row * w * 4,
                            IntPtr.Add(bd.Scan0, row * bd.Stride),
                            w * 4);
                }
                finally { _framebuffer.UnlockBits(bd); }
            }
        }

        private void ResizeFramebuffer(int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            lock (_fbLock)
            {
                _fbW = w; _fbH = h;
                var old = _framebuffer;
                _framebuffer = new Bitmap(w, h, PixelFormat.Format32bppRgb);
                old?.Dispose();
            }
        }

        // ------------------------------------------------------------------ authentication

        private void AuthVncPassword(string pass)
        {
            byte[] challenge = new byte[16];
            ReadFull(challenge);
            byte[] pw  = Encoding.ASCII.GetBytes(pass ?? "");
            byte[] key = new byte[8];
            for (int i = 0; i < 8; i++) key[i] = ReverseBits(i < pw.Length ? pw[i] : (byte)0);

            byte[] resp = new byte[16];
            using (var des = new DESCryptoServiceProvider { Key = key, Mode = CipherMode.ECB, Padding = PaddingMode.None })
            using (var enc = des.CreateEncryptor())
                enc.TransformBlock(challenge, 0, 16, resp, 0);
            _stream.Write(resp, 0, 16);
        }

        private void AuthMsLogon(string user, string domain, string pass)
        {
            ulong g = ReadUInt64BE(), p = ReadUInt64BE(), serverPub = ReadUInt64BE();

            byte[] rnd = new byte[8];
            new RNGCryptoServiceProvider().GetBytes(rnd);
            ulong clientPriv = (BitConverter.ToUInt64(rnd, 0) % ((1UL << 31) - 1)) + 1;
            ulong clientPub  = ModPow(g, clientPriv, p);
            ulong shared     = ModPow(serverPub, clientPriv, p);
            WriteUInt64BE(clientPub);

            // UltraVNC stores the shared key in little-endian byte order
            byte[] key = new byte[8];
            for (int i = 0; i < 8; i++) key[i] = (byte)(shared >> (8 * i));

            if (DESCryptoServiceProvider.IsWeakKey(key) || DESCryptoServiceProvider.IsSemiWeakKey(key))
                key[0] ^= 0x0F;

            string login = string.IsNullOrEmpty(domain) ? user : domain + "\\" + user;
            _stream.Write(VncEncryptBytes2(key, PadToSize(login, 256)), 0, 256);
            _stream.Write(VncEncryptBytes2(key, PadToSize(pass,   64)), 0, 64);
        }

        // vncEncryptBytes2: DES-ECB with manual CBC; first block IV = key; each call is independent
        private static byte[] VncEncryptBytes2(byte[] key8, byte[] data)
        {
            byte[] buf = (byte[])data.Clone();
            byte[] iv  = (byte[])key8.Clone(); // IV = key (per UltraVNC source)
            using (var des = new DESCryptoServiceProvider { Key = key8, Mode = CipherMode.ECB, Padding = PaddingMode.None })
            using (var enc = des.CreateEncryptor())
            {
                byte[] block = new byte[8];
                for (int i = 0; i < buf.Length; i += 8)
                {
                    for (int j = 0; j < 8; j++) buf[i + j] ^= iv[j];
                    enc.TransformBlock(buf, i, 8, block, 0);
                    Array.Copy(block, 0, buf, i, 8);
                    Array.Copy(block, 0, iv, 0, 8); // carry IV forward
                }
            }
            return buf;
        }

        private static byte ReverseBits(byte b)
        {
            byte r = 0;
            for (int i = 0; i < 8; i++) r |= (byte)(((b >> i) & 1) << (7 - i));
            return r;
        }

        private static ulong ModPow(ulong b, ulong e, ulong m) =>
            (ulong)BigInteger.ModPow(new BigInteger(b), new BigInteger(e), new BigInteger(m));

        private static byte[] PadToSize(string s, int size)
        {
            byte[] buf = new byte[size];
            byte[] raw = Encoding.ASCII.GetBytes(s ?? "");
            Array.Copy(raw, buf, Math.Min(raw.Length, size - 1));
            return buf;
        }

        // ------------------------------------------------------------------ IO helpers

        private void ReadFull(byte[] b)
        {
            int o = 0;
            while (o < b.Length)
            {
                int n = _stream.Read(b, o, b.Length - o);
                if (n <= 0) throw new System.IO.IOException("連線已中斷。");
                o += n;
            }
        }

        private void SkipBytes(int n) { if (n > 0) ReadFull(new byte[n]); }

        private int ReadUInt16BE()
        {
            byte[] b = new byte[2]; ReadFull(b); return (b[0] << 8) | b[1];
        }

        private uint ReadUInt32BE()
        {
            byte[] b = new byte[4]; ReadFull(b);
            return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        }

        private ulong ReadUInt64BE()
        {
            byte[] b = new byte[8]; ReadFull(b);
            ulong v = 0;
            for (int i = 0; i < 8; i++) v = (v << 8) | b[i];
            return v;
        }

        private void WriteUInt64BE(ulong v)
        {
            byte[] b = new byte[8];
            for (int i = 7; i >= 0; i--) { b[i] = (byte)v; v >>= 8; }
            _stream.Write(b, 0, 8);
        }

        private void SendFbUpdateRequest(bool inc)
        {
            if (_stream == null) return;
            byte[] r = { 3, (byte)(inc ? 1 : 0), 0, 0, 0, 0,
                         (byte)(_fbW >> 8), (byte)_fbW,
                         (byte)(_fbH >> 8), (byte)_fbH };
            try { _stream.Write(r, 0, 10); } catch { }
        }

        // ------------------------------------------------------------------ rendering

        protected override void OnPaint(PaintEventArgs e)
        {
            lock (_fbLock)
            {
                if (_framebuffer != null) e.Graphics.DrawImage(_framebuffer, ClientRectangle);
                else e.Graphics.Clear(Color.Black);
            }
        }

        // ------------------------------------------------------------------ mouse input

        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); SendPointerEvent(e); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); SendPointerEvent(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { base.OnMouseUp(e);   SendPointerEvent(e); }

        private void SendPointerEvent(MouseEventArgs e)
        {
            if (_state != 1) return;
            int x = _fbW > 0 ? e.X * _fbW / Math.Max(Width, 1) : 0;
            int y = _fbH > 0 ? e.Y * _fbH / Math.Max(Height, 1) : 0;
            byte mask = 0;
            if ((Control.MouseButtons & MouseButtons.Left)   != 0) mask |= 1;
            if ((Control.MouseButtons & MouseButtons.Middle) != 0) mask |= 2;
            if ((Control.MouseButtons & MouseButtons.Right)  != 0) mask |= 4;
            byte[] m = { 5, mask, (byte)(x >> 8), (byte)x, (byte)(y >> 8), (byte)y };
            try { _stream?.Write(m, 0, 6); } catch { }
        }

        // ------------------------------------------------------------------ keyboard input

        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); SendKey(e.KeyCode, true); }
        protected override void OnKeyUp(KeyEventArgs e)   { base.OnKeyUp(e);   SendKey(e.KeyCode, false); }

        private void SendKey(Keys k, bool down)
        {
            if (_state != 1 || !KeyMap.TryGetValue(k, out uint sym)) return;
            byte[] m = { 4, (byte)(down ? 1 : 0), 0, 0,
                         (byte)(sym >> 24), (byte)(sym >> 16), (byte)(sym >> 8), (byte)sym };
            try { _stream?.Write(m, 0, 8); } catch { }
        }

        private static readonly Dictionary<Keys, uint> KeyMap = new Dictionary<Keys, uint>
        {
            { Keys.A,0x61 }, { Keys.B,0x62 }, { Keys.C,0x63 }, { Keys.D,0x64 }, { Keys.E,0x65 },
            { Keys.F,0x66 }, { Keys.G,0x67 }, { Keys.H,0x68 }, { Keys.I,0x69 }, { Keys.J,0x6A },
            { Keys.K,0x6B }, { Keys.L,0x6C }, { Keys.M,0x6D }, { Keys.N,0x6E }, { Keys.O,0x6F },
            { Keys.P,0x70 }, { Keys.Q,0x71 }, { Keys.R,0x72 }, { Keys.S,0x73 }, { Keys.T,0x74 },
            { Keys.U,0x75 }, { Keys.V,0x76 }, { Keys.W,0x77 }, { Keys.X,0x78 }, { Keys.Y,0x79 },
            { Keys.Z,0x7A },
            { Keys.D0,0x30 }, { Keys.D1,0x31 }, { Keys.D2,0x32 }, { Keys.D3,0x33 }, { Keys.D4,0x34 },
            { Keys.D5,0x35 }, { Keys.D6,0x36 }, { Keys.D7,0x37 }, { Keys.D8,0x38 }, { Keys.D9,0x39 },
            { Keys.Return,0xFF0D }, { Keys.Escape,0xFF1B }, { Keys.Back,0xFF08 }, { Keys.Tab,0xFF09 },
            { Keys.Space,0x0020 }, { Keys.Delete,0xFFFF }, { Keys.Insert,0xFF63 },
            { Keys.Home,0xFF50 }, { Keys.End,0xFF57 }, { Keys.Prior,0xFF55 }, { Keys.Next,0xFF56 },
            { Keys.Left,0xFF51 }, { Keys.Up,0xFF52 }, { Keys.Right,0xFF53 }, { Keys.Down,0xFF54 },
            { Keys.F1,0xFFBE }, { Keys.F2,0xFFBF }, { Keys.F3,0xFFC0 }, { Keys.F4,0xFFC1 },
            { Keys.F5,0xFFC2 }, { Keys.F6,0xFFC3 }, { Keys.F7,0xFFC4 }, { Keys.F8,0xFFC5 },
            { Keys.F9,0xFFC6 }, { Keys.F10,0xFFC7 }, { Keys.F11,0xFFC8 }, { Keys.F12,0xFFC9 },
            { Keys.ShiftKey,0xFFE1 }, { Keys.LShiftKey,0xFFE1 }, { Keys.RShiftKey,0xFFE2 },
            { Keys.ControlKey,0xFFE3 }, { Keys.LControlKey,0xFFE3 }, { Keys.RControlKey,0xFFE4 },
            { Keys.Alt,0xFFE9 }, { Keys.LMenu,0xFFE9 }, { Keys.RMenu,0xFFEA },
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
