using System;

namespace RdpSolution.UI.RdpClient
{
    public sealed class RdpDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// The numeric disconnect reason code reported by the RDP client.
        /// Zero means the disconnect was initiated locally or the reason is unknown.
        /// </summary>
        public int Reason { get; }

        public RdpDisconnectedEventArgs(int reason)
        {
            Reason = reason;
        }

        /// <summary>Human-readable description of the most common disconnect codes.</summary>
        public string ReasonDescription
        {
            get
            {
                switch (Reason)
                {
                    case 0:    return "Session ended";
                    case 1:    return "User disconnected locally";
                    case 2:    return "Remote user disconnected";
                    case 3:    return "Server ended the session";
                    case 260:  return "DNS name lookup failure";
                    case 262:  return "Out of memory";
                    case 264:  return "Connection timed out";
                    case 516:  return "Could not connect to host";
                    case 520:  return "Host not found";
                    case 772:  return "Network error (send failed)";
                    case 788:  return "Decryption error";
                    case 1800: return "Socket closed unexpectedly";
                    case 2052: return "DNS lookup failed";
                    case 2308: return "Connection lost";
                    case 2311: return "Licensing protocol error";
                    case 2567: return "Disconnected by administrator";
                    case 2825: return "Session terminated by remote server";
                    default:   return "Disconnect code " + Reason;
                }
            }
        }
    }
}
