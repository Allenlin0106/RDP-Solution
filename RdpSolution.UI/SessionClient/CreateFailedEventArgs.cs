using System;

namespace RdpSolution.UI.SessionClient
{
    public sealed class CreateFailedEventArgs : EventArgs
    {
        public string Message { get; }
        public CreateFailedEventArgs(string message) { Message = message ?? string.Empty; }
    }
}
