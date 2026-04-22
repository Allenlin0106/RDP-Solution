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

            _receiveThread = new Thread(ConnectAndReceive) { IsBackground = true };
            _receiveThread.Start();
        }

        public void Disconnect()
        {
            _intentionalDisconnect = true;
            try { _tcp?.Close(); } catch { }
            _receiveThread?.Join(500);
            _state = 0;
        }

        // ------------------------------------------------------------------ background thread

        private void ConnectAndReceive()
        {
            try
            {
                _tcp    = new TcpClient();
                _tcp.Connect(_host.Hostname, _host.Port);
                _stream = _tcp.GetStream();

                Handshake();

                if (IsHandleCreated)
                    BeginInvoke(new Action(() => Connected?.Invoke(this, EventArgs.Empty)));
                _state = 1;

                ReceiveLoop();

                // Normal EOF — server closed the connection
                if (!_intentionalDisconnect && IsHandleCreated)
                    BeginInvoke(new Action(() =>
                        Disconnected?.Invoke(this,
                            new DisconnectedEventArgs("Server closed the connection."))));
            }
            catch (Exception ex)
            {
                if (!_intentionalDisconnect && IsHandleCreated)
                    BeginInvoke(new Action(() =>
                        Disconnected?.Invoke(this, new DisconnectedEventArgs(ex.Message))));
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

                bool hasType17 = Array.IndexOf(types, (byte)17) >= 0;
                bool hasType2  = Array.IndexOf(types, (byte)2)  >= 0;

                // Prefer type 17 for MS-Logon II; older UltraVNC may only offer type 2
                // even when MS-Logon II is configured, so fall through to type 2.
                byte chosen;
                if (_host.VncAuthType == VncAuthType.MsLogon)
                {
                    if (hasType17)     chosen = 17;
                    else if (hasType2) chosen = 2;
                    else               chosen = 1;
                }
                else
                {
                    if (hasType2)      chosen = 2;
                    else               chosen = 1;
                }

                _stream.WriteByte(chosen);

                if (chosen == 17)
                {
                    AuthMsLogon(
                        _host.Username ?? string.Empty,
                        _host.Domain   ?? string.Empty,
                        _host.Password ?? string.Empty);
                }
                else if (chosen == 2)
                {
                    if (_host.VncAuthType == VncAuthType.MsLogon)
                    {
                        // Older UltraVNC: MS-Logon II signalled over security type 2
                        AuthMsLogon(
                            _host.Username ?? string.Empty,
                            _host.Domain   ?? string.Empty,
                            _host.Password ?? string.Empty);
                    }
                    else
                    {
                        AuthVncPassword(_host.Password ?? string.Empty);
                        uint result = ReadUInt32BE();
                        if (result != 0)
                        {
                            uint reasonLen = ReadUInt32BE();
                            byte[] rb = new byte[reasonLen];
                            ReadFull(rb);
                            throw new InvalidOperationException(
                                "VNC authentication failed: " + Encoding.UTF8.GetString(rb));
                        }
                    }
                }
                // chosen == 1 (None): no auth body
            }
            else
            {
                // RFB 3.3: server dictates security type
                uint secType = ReadUInt32BE();
                if (secType == 2)
                {
                    AuthVncPassword(_host.Password ?? string.Empty);
                    uint result = ReadUInt32BE();
                    if (result != 0)
                        throw new InvalidOperationException("VNC authentication failed.");
                }
                else if (secType == 0)
                {
                    throw new InvalidOperationException("Server sent security failure.");
                }
            }

            // 3. ClientInit — shared=1
            _stream.WriteByte(1);

            // 4. ServerInit
            _fbW = ReadUInt16BE();
            _fbH = ReadUInt16BE();

            // Skip 16-byte pixel format
            byte[] pixFmt = new byte[16];
            ReadFull(pixFmt);

            uint nameLen = ReadUInt32BE();
            byte[] nameBytes = new byte[nameLen];
            ReadFull(nameBytes);

            lock (_fbLock)
                _framebuffer = new Bitmap(_fbW, _fbH, PixelFormat.Format32bppRgb);

            // 5. SetPixelFormat — request 32-bpp RGBX
            byte[] spf = new byte[20];
            spf[0] = 0;    // message type
            // 3 padding bytes
            spf[4] = 32;   // bits-per-pixel
            spf[5] = 24;   // depth
            spf[6] = 0;    // big-endian = false
            spf[7] = 1;    // true-colour = true
            // R max = 255
            spf[8] = 0; spf[9] = 255;
            // G max = 255
            spf[10] = 0; spf[11] = 255;
            // B max = 255
            spf[12] = 0; spf[13] = 255;
            spf[14] = 16;  // R shift
            spf[15] = 8;   // G shift
            spf[16] = 0;   // B shift
            _stream.Write(spf, 0, 20);

            // 6. SetEncodings — Raw only
            byte[] se = new byte[8];
            se[0] = 2;    // message type
            // 1 padding byte
            se[2] = 0; se[3] = 1;  // count = 1
            // encoding 0 = Raw (4 bytes, big-endian int32)
            _stream.Write(se, 0, 8);

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
                    case 0: HandleFramebufferUpdate(); break;
                    case 1: SkipColourMapEntries();    break;
                    case 2: /* Bell — no-op */         break;
                    case 3: SkipServerCutText();       break;
                    default:
                        throw new System.IO.IOException(
                            "Unsupported server message type " + msgType +
                            ". Stream is now corrupt; disconnecting.");
                }
            }
        }

        private void HandleFramebufferUpdate()
        {
            _stream.ReadByte(); // padding
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
                    case 0: // Raw — w*h pixels, 4 bytes each
                        if (w > 0 && h > 0)
                            ApplyRawRect(x, y, w, h);
                        break;

                    case -223: // DesktopSize pseudo-encoding — no pixel data; resize framebuffer
                        ResizeFramebuffer(w, h);
                        break;

                    case -239: // RichCursor — pixel data + bitmask; read and discard
                        if (w > 0 && h > 0)
                        {
                            ReadFull(new byte[w * h * 4]);              // cursor pixel data
                            ReadFull(new byte[((w + 7) / 8) * h]);      // bitmask
                        }
                        break;

                    case -240: // XCursor — fore+back bitmasks only
                        if (w > 0 && h > 0)
                            ReadFull(new byte[((w + 7) / 8) * h * 2]);  // two bitmasks
                        break;

                    case 1: // CopyRect — 4-byte source position
                        ReadFull(new byte[4]);
                        break;

                    default:
                        // Unknown encoding: cannot determine payload size; disconnect cleanly.
                        throw new System.IO.IOException(
                            "Unsupported rectangle encoding " + encoding +
                            ". Stream is now corrupt; disconnecting.");
                }
            }

            if (IsHandleCreated)
                BeginInvoke(new Action(Invalidate));
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
            _stream.ReadByte(); // padding
            ReadUInt16BE();     // firstColour (discard)
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
            req[0] = 3;                                      // message type
            req[1] = (byte)(incremental ? 1 : 0);
            // x=0, y=0
            req[6] = (byte)(_fbW >> 8); req[7] = (byte)_fbW;
            req[8] = (byte)(_fbH >> 8); req[9] = (byte)_fbH;
            _stream.Write(req, 0, 10);
        }

        // ------------------------------------------------------------------ VNC Auth (type 2)

        private void AuthVncPassword(string password)
        {
            byte[] challenge = new byte[16];
            ReadFull(challenge);

            // Build 8-byte DES key from password (ASCII, padded/truncated, bits reversed)
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
            // Receive DH parameters
            ulong g         = ReadUInt64BE();
            ulong p         = ReadUInt64BE();
            ulong serverPub = ReadUInt64BE();

            // Generate client private key
            byte[] rnd = new byte[8];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(rnd);
            ulong clientPriv = BitConverter.ToUInt64(rnd, 0);
            if (clientPriv == 0) clientPriv = 1;

            // Compute keys
            ulong clientPub = ModPow(g, clientPriv, p);
            ulong shared    = ModPow(serverPub, clientPriv, p);

            // Send client public key
            WriteUInt64BE(clientPub);

            // Derive DES key from shared secret (big-endian byte array)
            byte[] desKey = new byte[8];
            for (int i = 0; i < 8; i++)
                desKey[i] = (byte)(shared >> (56 - 8 * i));

            // Build and encrypt credential buffers
            string userField = string.IsNullOrEmpty(domain)
                ? username
                : domain + "\\" + username;

            byte[] encUser = DesEncryptCbc(desKey, PadToSize(userField, 256));
            byte[] encPass = DesEncryptCbc(desKey, PadToSize(password,  64));

            _stream.Write(encUser, 0, 256);
            _stream.Write(encPass, 0,  64);

            // Read result
            uint result = ReadUInt32BE();
            if (result != 0)
                throw new InvalidOperationException("MS-Logon II authentication failed.");
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
            byte[] raw = Encoding.UTF8.GetBytes(s ?? string.Empty);
            Array.Copy(raw, buf, Math.Min(raw.Length, size - 1));
            return buf;
        }

        private static byte[] DesEncryptCbc(byte[] key8, byte[] data)
        {
            using (var des = new DESCryptoServiceProvider())
            {
                des.Key     = key8;
                des.IV      = new byte[8];
                des.Mode    = CipherMode.CBC;
                des.Padding = PaddingMode.None;
                using (var enc = des.CreateEncryptor())
                    return enc.TransformFinalBlock(data, 0, data.Length);
            }
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
            if (_state == 1) SendPointerEvent(e, 0);
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
            try { _stream?.Write(msg, 0, 6); } catch { }
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
            // 2 padding bytes
            msg[4] = (byte)(keysym >> 24);
            msg[5] = (byte)(keysym >> 16);
            msg[6] = (byte)(keysym >>  8);
            msg[7] = (byte) keysym;
            try { _stream?.Write(msg, 0, 8); } catch { }
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
