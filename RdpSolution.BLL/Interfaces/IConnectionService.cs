using RdpSolution.DAL.Models;

namespace RdpSolution.BLL.Interfaces
{
    public interface IConnectionService
    {
        /// <summary>
        /// Returns the text content of a standard .rdp configuration file for the
        /// given host, useful for export or saving to disk.
        /// </summary>
        string GenerateRdpFileContent(RemoteHostConfig host);
    }
}
