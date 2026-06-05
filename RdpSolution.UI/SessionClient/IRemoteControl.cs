using System;
using RdpSolution.DAL.Models;

namespace RdpSolution.UI.SessionClient
{
    public interface IRemoteControl
    {
        /// <summary>0 = not connected, 1 = connected, 2 = connecting.</summary>
        int ConnectedState { get; }

        event EventHandler Connected;
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<CreateFailedEventArgs> CreateFailed;

        void Connect(RemoteHostConfig host);
        void Disconnect();
    }
}
