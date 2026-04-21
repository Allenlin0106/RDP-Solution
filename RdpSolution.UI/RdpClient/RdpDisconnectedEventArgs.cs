using System;
using MSTSCLib;

namespace RdpSolution.UI.RdpClient
{
    public sealed class RdpDisconnectedEventArgs : EventArgs
    {
        public int Reason         { get; }
        public int ExtendedReason { get; }

        public RdpDisconnectedEventArgs(int reason, int extendedReason = 0)
        {
            Reason         = reason;
            ExtendedReason = extendedReason;
        }

        public string ReasonDescription
        {
            get
            {
                // Prefer the typed extended reason when the control supplies one.
                if (ExtendedReason != 0)
                {
                    switch ((ExtendedDisconnectReasonCode)ExtendedReason)
                    {
                        case ExtendedDisconnectReasonCode.exDiscReasonNoInfo:
                            break; // fall through to basic reason

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
                            return "Licensing error";

                        default:
                            return "Disconnected (extended code " + ExtendedReason + ")";
                    }
                }

                // Basic discReason fallback.
                switch (Reason)
                {
                    case 0: return "Session ended";
                    case 1: return "User disconnected locally";
                    case 2: return "Remote user disconnected";
                    case 3: return "Server ended the session";
                    default: return "Disconnected (code " + Reason + ")";
                }
            }
        }
    }
}
