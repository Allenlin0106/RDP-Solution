using System.Collections.Generic;
using RdpSolution.DAL.Models;

namespace RdpSolution.DAL.Interfaces
{
    public interface IHostConfigRepository
    {
        /// <summary>Returns all host entries.</summary>
        IList<RemoteHostConfig> GetAll();

        /// <summary>Returns the host with the given id, or null.</summary>
        RemoteHostConfig GetById(string id);

        /// <summary>Appends a new host. Assigns an Id if one is not set.</summary>
        void Add(RemoteHostConfig config);

        /// <summary>Replaces the stored entry that shares config.Id.</summary>
        void Update(RemoteHostConfig config);

        /// <summary>Removes the host with the given id (no-op if not found).</summary>
        void Delete(string id);

        /// <summary>Persists all in-memory changes back to the backing store.</summary>
        void Save();
    }
}
