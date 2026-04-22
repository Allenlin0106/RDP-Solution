using System;

namespace RdpSolution.UI.SessionClient
{
    public sealed class DisconnectedEventArgs : EventArgs
    {
        public string Reason { get; }
        public DisconnectedEventArgs(string reason) { Reason = reason ?? string.Empty; }
    }
}
