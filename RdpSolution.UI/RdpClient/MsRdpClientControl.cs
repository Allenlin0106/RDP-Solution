using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;

namespace RdpSolution.UI.RdpClient
{
    /// <summary>
    /// Wraps <see cref="AxMsRdpClient9NotSafeForScripting"/> with a clean public API
    /// that matches the rest of the UI layer.  The inner AxHost is created lazily in
    /// <see cref="OnHandleCreated"/> so that any COM registration failure is caught and
    /// surfaced via <see cref="CreateFailed"/> rather than propagating as an unhandled
    /// exception.
    /// </summary>
    public sealed class MsRdpClientControl : UserControl
    {
        private InternalRdpClient _axRdp;
        private bool _initialized;

        // ------------------------------------------------------------------ events

        /// <summary>Fired when the RDP session becomes fully connected.</summary>
        public event EventHandler RdpConnected;

        /// <summary>Fired when the RDP session disconnects.</summary>
        public event EventHandler<RdpDisconnectedEventArgs> RdpDisconnected;

        /// <summary>
        /// Fired when the COM ActiveX control cannot be created (e.g. REGDB_E_CLASSNOTREG).
        /// When this event fires <see cref="Connect"/> and all property setters become no-ops.
        /// </summary>
        public event EventHandler<CreateFailedEventArgs> CreateFailed;

        // ------------------------------------------------------------------ state

        /// <summary>0 = not connected, 1 = connected, 2 = connecting.</summary>
        public int ConnectedState => _initialized ? _axRdp.Connected : 0;

        // ------------------------------------------------------------------ connection properties

        public string Server        { set { if (_initialized) _axRdp.Server        = value; } }
        public string Domain        { set { if (_initialized) _axRdp.Domain        = value; } }
        public string UserName      { set { if (_initialized) _axRdp.UserName      = value; } }
        public int    DesktopWidth  { set { if (_initialized) _axRdp.DesktopWidth  = value; } }
        public int    DesktopHeight { set { if (_initialized) _axRdp.DesktopHeight = value; } }
        public bool   FullScreen    { set { if (_initialized) _axRdp.FullScreen    = value; } }

        // ------------------------------------------------------------------ advanced settings

        public void SetPort(int port)
        {
            if (_initialized) _axRdp.AdvancedSettings9.RDPPort = port;
        }

        public void SetRedirectDrives(bool v)
        {
            if (_initialized) _axRdp.AdvancedSettings9.RedirectDrives = v;
        }

        public void SetRedirectPrinters(bool v)
        {
            if (_initialized) _axRdp.AdvancedSettings9.RedirectPrinters = v;
        }

        public void SetRedirectClipboard(bool v)
        {
            if (_initialized) _axRdp.AdvancedSettings9.RedirectClipboard = v;
        }

        public void SetSmartResize(bool v)
        {
            if (_initialized) _axRdp.AdvancedSettings9.SmartSizing = v;
        }

        // ------------------------------------------------------------------ password

        public void SetPassword(string password)
        {
            if (!_initialized || string.IsNullOrEmpty(password)) return;
            try
            {
                // IMsTscNonScriptable is IUnknown-only; QueryInterface via COM cast.
                var ns = _axRdp.GetOcx() as IMsTscNonScriptable;
                if (ns != null) ns.ClearTextPassword = password;
            }
            catch { }
        }

        // ------------------------------------------------------------------ actions

        public void Connect()
        {
            if (_initialized) _axRdp.Connect();
        }

        public void Disconnect()
        {
            if (_initialized && _axRdp.Connected != 0) _axRdp.Disconnect();
        }

        // ------------------------------------------------------------------ AxHost lifecycle

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                _axRdp = new InternalRdpClient { Dock = DockStyle.Fill };
                _axRdp.OnConnected    += (s, ev) => RdpConnected?.Invoke(this, EventArgs.Empty);
                _axRdp.OnDisconnected += (s, ev) =>
                    RdpDisconnected?.Invoke(this, new RdpDisconnectedEventArgs(
                        ev.discReason, (int)_axRdp.ExtendedDisconnectReason));

                Controls.Add(_axRdp);   // triggers AxHost.CreateHandle() synchronously
                _initialized = true;
            }
            catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x80040154))
            {
                CreateFailed?.Invoke(this, new CreateFailedEventArgs(
                    "The Remote Desktop ActiveX control (MsTscAx.dll) is not registered.\n\n" +
                    "Ensure the application targets x86 (32-bit)."));
            }
            catch (Exception ex)
            {
                CreateFailed?.Invoke(this, new CreateFailedEventArgs(ex.Message));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _axRdp?.Dispose();
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ inner helper

        // Exposes the protected AxHost.GetOcx() so we can QueryInterface for
        // IUnknown-only interfaces (e.g. IMsTscNonScriptable) that are not
        // reachable through the AxMSTSCLib typed surface.
        private sealed class InternalRdpClient : AxMsRdpClient9NotSafeForScripting
        {
            public new object GetOcx() => base.GetOcx();
        }
    }

    public sealed class CreateFailedEventArgs : EventArgs
    {
        public string Message { get; }
        public CreateFailedEventArgs(string message) { Message = message; }
    }
}
