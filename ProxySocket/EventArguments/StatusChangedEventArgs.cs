using System;

namespace ProxySocket.EventArguments
{
    public sealed class StatusChangedEventArgs : EventArgs
    {
        public string Status { get { return _status; } }

        private string _status;

        public StatusChangedEventArgs(string status) : base()
        {
            _status = status;
        }
    }
}