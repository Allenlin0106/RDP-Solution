using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;
using RdpSolution.DAL.Models;
using RdpSolution.UI.SessionClient;

namespace RdpSolution.UI.RdpClient
{
    /// <summary>
    /// Wraps <see cref="AxMsRdpClient9NotSafeForScripting"/> with a clean public API
    /// that implements <see cref="IRemoteControl"/>. The inner AxHost is created lazily
    /// in <see cref="OnHandleCreated"/> so that any COM registration failure is caught
    /// and surfaced via <see cref="CreateFailed"/> rather than propagating as an unhandled
    /// exception.
    /// </summary>
    public sealed class MsRdpClientControl : UserControl, IRemoteControl
    {
        private InternalRdpClient _axRdp;
        private bool _initialized;

        // ------------------------------------------------------------------ IRemoteControl events

        public event EventHandler Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<CreateFailedEventArgs> CreateFailed;

        // ------------------------------------------------------------------ IRemoteControl state

        public int ConnectedState => _initialized ? _axRdp.Connected : 0;

        // ------------------------------------------------------------------ IRemoteControl actions

        public void Connect(RemoteHostConfig host)
        {
            if (!_initialized) return;

            _axRdp.Server        = host.Hostname ?? string.Empty;
            _axRdp.Domain        = host.Domain   ?? string.Empty;
            _axRdp.UserName      = host.Username ?? string.Empty;
            _axRdp.DesktopWidth  = host.Width;
            _axRdp.DesktopHeight = host.Height;
            _axRdp.FullScreen    = false;
            _axRdp.AdvancedSettings9.RDPPort         = host.Port;
            _axRdp.AdvancedSettings9.RedirectDrives   = host.AttachDrives;
            _axRdp.AdvancedSettings9.RedirectPrinters = host.AttachPrinters;
            _axRdp.AdvancedSettings9.RedirectClipboard = true;
            _axRdp.AdvancedSettings9.SmartSizing       = true;

            if (!string.IsNullOrEmpty(host.Password))
            {
                try
                {
                    var ns = _axRdp.GetOcx() as IMsTscNonScriptable;
                    if (ns != null) ns.ClearTextPassword = host.Password;
                }
                catch { }
            }

            _axRdp.Connect();
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
                _axRdp.OnConnected    += (s, ev) => Connected?.Invoke(this, EventArgs.Empty);
                _axRdp.OnDisconnected += (s, ev) =>
                {
                    string reason = TranslateReason(ev.discReason, (int)_axRdp.ExtendedDisconnectReason);
                    Disconnected?.Invoke(this, new DisconnectedEventArgs(reason));
                };

                Controls.Add(_axRdp);
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

        // ------------------------------------------------------------------ reason translation

        private static string TranslateReason(int discReason, int extReason)
        {
            if (extReason != 0)
            {
                switch ((ExtendedDisconnectReasonCode)extReason)
                {
                    case ExtendedDisconnectReasonCode.exDiscReasonAPIInitiatedDisconnect:
                        return "Disconnected";
                    case ExtendedDisconnectReasonCode.exDiscReasonAPIInitiatedLogoff:
                        return "Logged off";
                    case ExtendedDisconnectReasonCode.exDiscReasonServerIdleTimeout:
                        return "Idle timeout";
                    case ExtendedDisconnectReasonCode.exDiscReasonServerLogonTimeout:
                        return "Logon timeout";
                    case ExtendedDisconnectReasonCode.exDiscReasonReplacedByOtherConnection:
                        return "Replaced by another connection";
                    case ExtendedDisconnectReasonCode.exDiscReasonOutOfMemory:
                        return "Out of memory";
                    case ExtendedDisconnectReasonCode.exDiscReasonServerDeniedConnection:
                        return "Server denied the connection";
                    case ExtendedDisconnectReasonCode.exDiscReasonServerDeniedConnectionFips:
                        return "Server denied the connection (FIPS policy)";
                    case ExtendedDisconnectReasonCode.exDiscReasonServerInsufficientPrivileges:
                        return "Insufficient privileges";
                    case ExtendedDisconnectReasonCode.exDiscReasonServerFreshCredentialsRequired:
                        return "Fresh credentials required";
                    case ExtendedDisconnectReasonCode.exDiscReasonRPCInitiatedDisconnectByUser:
                        return "Disconnected by user";
                    case ExtendedDisconnectReasonCode.exDiscReasonLogoffByUser:
                        return "Logged off by user";
                    case (ExtendedDisconnectReasonCode)768: // exDiscReasonRdpEncInvalidCredentials
                        return "Invalid credentials";
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseInternal:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseNoLicenseServer:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseNoLicense:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseErrClientMsg:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseHwidDoesntMatchLicense:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseErrClientLicense:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseCantFinishProtocol:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseClientEndedProtocol:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseErrClientEncryption:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseCantUpgradeLicense:
                    case ExtendedDisconnectReasonCode.exDiscReasonLicenseNoRemoteConnections:
                    case (ExtendedDisconnectReasonCode)267: // exDiscReasonLicenseCreatingLicStoreAccDenied
                        return "Licensing error";
                    default:
                        return "Disconnected (extended code " + extReason + ")";
                }
            }

            switch (discReason)
            {
                case 0:    return "Session ended";
                case 1:    return "User disconnected locally";
                case 2:    return "Remote user disconnected";
                case 3:    return "Server ended the session";
                case 260:  return "DNS name lookup failure";
                case 264:  return "Connection timed out";
                case 516:  return "Connection failed";
                case 520:  return "Host not found";
                case 772:  return "Connection timed out";
                case 1028: return "DNS name lookup failure";
                case 2055: return "Logon failed";
                case 2311: return "Remote Desktop Services not enabled on server";
                case 2567: return "Decryption error";
                case 2823: return "Account disabled";
                case 3079: return "Account restriction";
                case 3335: return "Account locked out";
                case 3591: return "Account expired";
                case 3847: return "Password expired";
                case 4615: return "Password must change";
                case 5639: return "Logon failure";
                default:   return "Disconnected (code " + discReason + ")";
            }
        }

        // ------------------------------------------------------------------ inner helper

        private sealed class InternalRdpClient : AxMsRdpClient9NotSafeForScripting
        {
            public new object GetOcx() => base.GetOcx();
        }
    }
}
