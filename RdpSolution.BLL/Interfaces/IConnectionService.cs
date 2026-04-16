using RdpSolution.DAL.Models;

namespace RdpSolution.BLL.Interfaces
{
    public interface IConnectionService
    {
        /// <summary>
        /// Launches an RDP session to the supplied host using the system mstsc.exe client.
        /// </summary>
        void Connect(RemoteHostConfig host);

        /// <summary>
        /// Returns the text content of a standard .rdp file for the given host,
        /// useful for export or preview.
        /// </summary>
        string GenerateRdpFileContent(RemoteHostConfig host);
    }
}
