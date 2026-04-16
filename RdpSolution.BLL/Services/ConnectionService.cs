using System;
using System.Text;
using RdpSolution.BLL.Interfaces;
using RdpSolution.DAL.Models;

namespace RdpSolution.BLL.Services
{
    /// <summary>
    /// Builds a standard .rdp file from a <see cref="RemoteHostConfig"/> and launches
    /// the system mstsc.exe client to initiate the connection.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        // ------------------------------------------------------------------ public API

        public string GenerateRdpFileContent(RemoteHostConfig host)
        {
            if (host == null) throw new ArgumentNullException("host");

            var sb = new StringBuilder();

            // Session geometry
            sb.AppendLine("screen mode id:i:" + (host.FullScreen ? "2" : "1"));
            sb.AppendLine("use multimon:i:0");
            sb.AppendLine("desktopwidth:i:"  + host.Width);
            sb.AppendLine("desktopheight:i:" + host.Height);
            sb.AppendLine("session bpp:i:"   + host.ColorDepth);

            // Network / performance
            sb.AppendLine("compression:i:1");
            sb.AppendLine("connection type:i:7");
            sb.AppendLine("networkautodetect:i:1");
            sb.AppendLine("bandwidthautodetect:i:1");
            sb.AppendLine("displayconnectionbar:i:1");

            // Visual quality
            sb.AppendLine("disable wallpaper:i:0");
            sb.AppendLine("allow font smoothing:i:0");
            sb.AppendLine("allow desktop composition:i:0");
            sb.AppendLine("disable full window drag:i:1");
            sb.AppendLine("disable menu anims:i:1");
            sb.AppendLine("disable themes:i:0");
            sb.AppendLine("bitmapcachepersistenable:i:1");

            // Target host  – append non-standard port as "host:port"
            var address = host.Port == 3389
                ? host.Hostname
                : host.Hostname + ":" + host.Port;
            sb.AppendLine("full address:s:" + address);

            // Credentials
            if (!string.IsNullOrEmpty(host.Username))
            {
                var user = string.IsNullOrEmpty(host.Domain)
                    ? host.Username
                    : host.Domain + "\\" + host.Username;
                sb.AppendLine("username:s:" + user);
                sb.AppendLine("domain:s:"   + (host.Domain ?? string.Empty));
            }

            // Audio / keyboard
            sb.AppendLine("audiomode:i:0");
            sb.AppendLine("audiocapturemode:i:0");
            sb.AppendLine("keyboardhook:i:2");

            // Device redirection
            sb.AppendLine("redirectprinters:i:"  + (host.AttachPrinters ? "1" : "0"));
            sb.AppendLine("redirectclipboard:i:1");
            sb.AppendLine("redirectsmartcards:i:1");
            sb.AppendLine("redirectcomports:i:0");
            sb.AppendLine("redirectposdevices:i:0");
            sb.AppendLine("drivestoredirect:s:"  + (host.AttachDrives ? "*" : ""));

            // Security
            sb.AppendLine("authentication level:i:2");
            sb.AppendLine("negotiate security layer:i:1");
            sb.AppendLine("prompt for credentials:i:0");
            sb.AppendLine("autoreconnection enabled:i:1");

            // Gateway (disabled)
            sb.AppendLine("gatewayhostname:s:");
            sb.AppendLine("gatewayusagemethod:i:4");
            sb.AppendLine("gatewaycredentialssource:i:4");
            sb.AppendLine("gatewayprofileusagemethod:i:0");
            sb.AppendLine("promptcredentialonce:i:0");

            // Misc
            sb.AppendLine("remoteapplicationmode:i:0");
            sb.AppendLine("alternate shell:s:");
            sb.AppendLine("shell working directory:s:");

            return sb.ToString();
        }
    }
}
