namespace RdpSolution.DAL.Models
{
    /// <summary>
    /// Represents one remote-desktop host entry as read from the configuration file.
    /// </summary>
    public class RemoteHostConfig
    {
        public string Id { get; set; }

        /// <summary>Friendly display name shown in the UI.</summary>
        public string Name { get; set; }

        /// <summary>DNS hostname or IP address of the remote machine.</summary>
        public string Hostname { get; set; }

        /// <summary>TCP port for the RDP listener. Default is 3389.</summary>
        public int Port { get; set; }

        public string Username { get; set; }

        /// <summary>Windows / AD domain. Leave empty for local accounts.</summary>
        public string Domain { get; set; }

        /// <summary>Desired remote desktop width in pixels (used when FullScreen is false).</summary>
        public int Width { get; set; }

        /// <summary>Desired remote desktop height in pixels (used when FullScreen is false).</summary>
        public int Height { get; set; }

        public bool FullScreen { get; set; }

        /// <summary>Redirect local drives into the remote session.</summary>
        public bool AttachDrives { get; set; }

        /// <summary>Redirect local printers into the remote session.</summary>
        public bool AttachPrinters { get; set; }

        /// <summary>Color depth in bits-per-pixel: 8, 15, 16, 24, or 32.</summary>
        public int ColorDepth { get; set; }

        public string Notes { get; set; }

        public RemoteHostConfig()
        {
            Port      = 3389;
            Width     = 1024;
            Height    = 768;
            ColorDepth = 32;
        }
    }
}
