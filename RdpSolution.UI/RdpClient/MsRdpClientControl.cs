using System;
using System.Reflection;
using System.Windows.Forms;

namespace RdpSolution.UI.RdpClient
{
    /// <summary>
    /// Hosts the Windows built-in RDP ActiveX control (MsTscAx.dll) inside a WinForms
    /// control without requiring pre-generated AxInterop assemblies.
    ///
    /// All property access is performed through reflection / late-binding so the binary
    /// ships without a COM reference or additional interop DLL.  The best available client
    /// version (v10 → v9 → v8 → v7) is chosen automatically at class load time.
    /// </summary>
    public sealed class MsRdpClientControl : AxHost
    {
        // Resolve the CLSID once, before any instance is constructed.
        private static readonly string s_clsid = ResolveClsid();

        private static string ResolveClsid()
        {
            string[] progIds =
            {
                "MsTscAx.MsRdpClient10NotSafeForScripting",
                "MsTscAx.MsRdpClient9NotSafeForScripting",
                "MsTscAx.MsRdpClient8NotSafeForScripting",
                "MsTscAx.MsRdpClient7NotSafeForScripting",
            };
            foreach (var pid in progIds)
            {
                var t = Type.GetTypeFromProgID(pid, false);
                if (t != null)
                    return t.GUID.ToString("B").ToUpperInvariant();
            }
            // Hard-coded fallback: MsRdpClient9NotSafeForScripting (Windows 7 SP1+)
            return "{301B94BA-5D25-4A12-BFE3-DE2C75CC7571}";
        }

        // ------------------------------------------------------------------ state

        private object _rdp;   // raw COM OCX
        private object _adv;   // cached AdvancedSettings* object
        private Timer  _pollTimer;
        private int    _lastState = -1;

        // ------------------------------------------------------------------ ctor / AxHost

        public MsRdpClientControl() : base(s_clsid)
        {
            TabStop = false;
        }

        protected override void AttachInterfaces()
        {
            _rdp = GetOcx();
            _adv = ResolveAdvancedSettings();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            StartPollTimer();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _pollTimer?.Dispose();
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ main properties

        public string Server
        {
            get => Get<string>("Server");
            set => Set("Server", value);
        }

        public string Domain
        {
            get => Get<string>("Domain");
            set => Set("Domain", value);
        }

        public string UserName
        {
            get => Get<string>("UserName");
            set => Set("UserName", value);
        }

        public int DesktopWidth
        {
            get => Get<int>("DesktopWidth");
            set => Set("DesktopWidth", value);
        }

        public int DesktopHeight
        {
            get => Get<int>("DesktopHeight");
            set => Set("DesktopHeight", value);
        }

        public bool FullScreen
        {
            get => Get<bool>("FullScreen");
            set => Set("FullScreen", value);
        }

        /// <summary>0 = not connected, 1 = connected, 2 = connecting.</summary>
        public int ConnectedState => Get<int>("Connected");

        // ------------------------------------------------------------------ advanced settings

        public void SetPort(int port)              => SetAdv("RDPPort",           port);
        public void SetRedirectDrives(bool v)      => SetAdv("RedirectDrives",    v);
        public void SetRedirectPrinters(bool v)    => SetAdv("RedirectPrinters",  v);
        public void SetRedirectClipboard(bool v)   => SetAdv("RedirectClipboard", v);
        public void SetSmartResize(bool v)         => SetAdv("SmartSizing",       v);

        /// <summary>
        /// Supplies a plaintext password via IMsTscNonScriptable.put_ClearTextPassword.
        /// This interface is IUnknown-only (not IDispatch), so the CLR QueryInterfaces
        /// for it via the [ComImport] cast rather than through reflection.
        /// </summary>
        public void SetPassword(string password)
        {
            if (_rdp == null || string.IsNullOrEmpty(password)) return;
            try
            {
                var ns = _rdp as IMsTscNonScriptable;
                ns?.put_ClearTextPassword(password);
            }
            catch { }
        }

        // ------------------------------------------------------------------ actions

        public void Connect()
        {
            if (_rdp == null) return;
            Invoke("Connect");
        }

        public void Disconnect()
        {
            if (_rdp == null) return;
            Invoke("Disconnect");
        }

        // ------------------------------------------------------------------ events (polled)

        /// <summary>Raised on the UI thread when the session becomes fully connected.</summary>
        public event EventHandler RdpConnected;

        /// <summary>Raised on the UI thread when the session disconnects.</summary>
        public event EventHandler<RdpDisconnectedEventArgs> RdpDisconnected;

        private void StartPollTimer()
        {
            _pollTimer = new Timer { Interval = 400 };
            _pollTimer.Tick += (s, e) =>
            {
                if (_rdp == null) return;

                int state = ConnectedState;
                if (state == _lastState) return;

                int prev = _lastState;
                _lastState = state;

                if (state == 1)
                {
                    RdpConnected?.Invoke(this, EventArgs.Empty);
                }
                else if (state == 0 && prev != -1)
                {
                    // Transition to disconnected (ignore the initial -1→0 at startup)
                    int reason = prev == 1 ? Get<int>("DisconnectedReason") : 0;
                    RdpDisconnected?.Invoke(this, new RdpDisconnectedEventArgs(reason));
                }
            };
            _pollTimer.Start();
        }

        // ------------------------------------------------------------------ reflection helpers

        private T Get<T>(string name)
        {
            if (_rdp == null) return default(T);
            try
            {
                object v = _rdp.GetType().InvokeMember(
                    name, BindingFlags.GetProperty, null, _rdp, null);
                if (v == null) return default(T);
                return (T)Convert.ChangeType(v, typeof(T));
            }
            catch { return default(T); }
        }

        private void Set(string name, object value)
        {
            if (_rdp == null) return;
            try
            {
                _rdp.GetType().InvokeMember(
                    name, BindingFlags.SetProperty, null, _rdp, new[] { value });
            }
            catch { }
        }

        private void Invoke(string name)
        {
            if (_rdp == null) return;
            try
            {
                _rdp.GetType().InvokeMember(
                    name, BindingFlags.InvokeMethod, null, _rdp, null);
            }
            catch { }
        }

        private object ResolveAdvancedSettings()
        {
            if (_rdp == null) return null;
            // Try newest → oldest to get the richest interface.
            string[] candidates =
            {
                "AdvancedSettings9", "AdvancedSettings8", "AdvancedSettings7",
                "AdvancedSettings6", "AdvancedSettings5", "AdvancedSettings4",
                "AdvancedSettings3", "AdvancedSettings2"
            };
            foreach (var p in candidates)
            {
                try
                {
                    object v = _rdp.GetType().InvokeMember(
                        p, BindingFlags.GetProperty, null, _rdp, null);
                    if (v != null) return v;
                }
                catch { }
            }
            return null;
        }

        private void SetAdv(string name, object value)
        {
            if (_adv == null)
                _adv = ResolveAdvancedSettings();
            if (_adv == null) return;
            try
            {
                _adv.GetType().InvokeMember(
                    name, BindingFlags.SetProperty, null, _adv, new[] { value });
            }
            catch { }
        }
    }
}
