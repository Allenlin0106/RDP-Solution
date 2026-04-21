using System;
using System.Collections.Generic;
using RdpSolution.BLL.Interfaces;
using RdpSolution.DAL.Interfaces;
using RdpSolution.DAL.Models;
using RdpSolution.DAL.Repositories;

namespace RdpSolution.BLL.Services
{
    public class HostService : IHostService
    {
        private readonly IHostConfigRepository _repository;

        public HostService(string configFilePath)
        {
            _repository = new XmlHostConfigRepository(configFilePath);
        }

        public IList<RemoteHostConfig> GetAll()          => _repository.GetAll();
        public RemoteHostConfig        GetById(string id) => _repository.GetById(id);
        public void                    Delete(string id)  => _repository.Delete(id);
        public void                    Save()             => _repository.Save();

        public void Add(RemoteHostConfig host)
        {
            Validate(host);
            _repository.Add(host);
        }

        public void Update(RemoteHostConfig host)
        {
            Validate(host);
            _repository.Update(host);
        }

        // ------------------------------------------------------------------ validation

        private static void Validate(RemoteHostConfig host)
        {
            if (host == null)
                throw new ArgumentNullException("host");
            if (string.IsNullOrWhiteSpace(host.Name))
                throw new ArgumentException("Host name is required.");
            if (string.IsNullOrWhiteSpace(host.Hostname))
                throw new ArgumentException("Hostname or IP address is required.");
            if (host.Port < 1 || host.Port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535.");
            if (host.ColorDepth != 8  && host.ColorDepth != 15 &&
                host.ColorDepth != 16 && host.ColorDepth != 24 && host.ColorDepth != 32)
                throw new ArgumentException("Color depth must be 8, 15, 16, 24, or 32.");
        }
    }
}
