using System.Collections.Generic;
using RdpSolution.DAL.Models;

namespace RdpSolution.BLL.Interfaces
{
    public interface IHostService
    {
        IList<RemoteHostConfig> GetAll();
        RemoteHostConfig GetById(string id);

        /// <summary>Validates then adds the host. Throws <see cref="System.ArgumentException"/> on invalid data.</summary>
        void Add(RemoteHostConfig host);

        /// <summary>Validates then replaces the stored entry. Throws <see cref="System.ArgumentException"/> on invalid data.</summary>
        void Update(RemoteHostConfig host);

        void Delete(string id);
        void Save();
    }
}
