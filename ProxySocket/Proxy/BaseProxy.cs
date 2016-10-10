using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ProxySocket.EventArguments;
using ProxySocket.Objects;

namespace ProxySocket.Proxy
{
    public delegate void ConnectedEventHandler(object sender, EventArgs e);
    public delegate void DisconnectedEventHandler(object sender, EventArgs e);
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

    public abstract class BaseProxy : IDisposable
    {
        #region Properties
        public bool Connected { get { return _connected; } }
        public IPEndPoint Proxy { get { return _proxyEndPoint; } }
        public IPEndPoint Target { get { return _targetEndPoint; } }
        public string Username { get; set; }
        public string Password { get; set; }
        public int TimeOut { get; set; } = 5000;
        #endregion

        #region Events
        public event ConnectedEventHandler OnConnected;
        public event DisconnectedEventHandler OnDisconnected;
        public event StatusChangedEventHandler OnStatusChanged;
        #endregion

        #region Fields
        protected bool _connected;
        protected Socket _socket;
        protected IPEndPoint _proxyEndPoint;
        protected IPEndPoint _targetEndPoint;
        protected byte[] buffer;
        protected bool _disposed;
        #endregion

        #region Constructors
        public BaseProxy(string proxyHost, int proxyPort, string targetHost, int targetPort)
        {
            IPAddress proxyAddress;
            if (!IPAddress.TryParse(proxyHost, out proxyAddress))
                proxyAddress = Dns.GetHostAddresses(proxyHost).FirstOrDefault();

            IPAddress targetAddress;
            if (!IPAddress.TryParse(targetHost, out targetAddress))
                targetAddress = Dns.GetHostAddresses(targetHost).FirstOrDefault();

            _proxyEndPoint = new IPEndPoint(proxyAddress, proxyPort);
            _targetEndPoint = new IPEndPoint(targetAddress, targetPort);
        }

        public BaseProxy(IPAddress proxyAddress, int proxyPort, IPAddress targetAddress, int targetPort)
        {
            _proxyEndPoint = new IPEndPoint(proxyAddress, proxyPort);
            _targetEndPoint = new IPEndPoint(targetAddress, targetPort);
        }

        public BaseProxy(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint)
        {
            _proxyEndPoint = proxyEndPoint;
            _targetEndPoint = targetEndPoint;
        }
        #endregion

        #region Connection Methods
        public void Connect()
        {
            Socket socket = BuildSocket();
            _socket.Connect(_proxyEndPoint);
            InvokeConnected();
        }

        public void BeginConnect(AsyncCallback callback, object state)
        {
            Socket socket = BuildSocket();
            _socket.BeginConnect(_proxyEndPoint, callback, state);
        }
        
        private Socket BuildSocket()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = TimeOut;
            _socket.SendTimeout = TimeOut;
            return _socket;
        }
        #endregion

        #region Packet Methods
        public void SendPacket(Packet packet)
        {
            byte[] buffer = packet.Buffer;
            _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void ReceivePacket(int size = 1024)
        {
            buffer = new byte[size];
            _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public void SendPacketAsync(Packet packet, AsyncCallback callback, object state)
        {
            buffer = packet.Buffer;
            _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, callback, state);
        }

        public void ReceivePacketAsync(SocketFlags socketFlags, AsyncCallback callback, object state, int size)
        {
            buffer = new byte[size];
            _socket.BeginReceive(buffer, 0, size, socketFlags, callback, state);
        }

        protected void EndSend(string status, IAsyncResult result)
        {
            _socket.EndSend(result);
            InvokeStatus(status);
        }
        #endregion

        #region Disconnection Methods
        public void Disconnect()
        {
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(true);
                InvokeDisconnected();
            }
        }

        public void DisconnectAsync()
        {
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.BeginDisconnect(true, new AsyncCallback(OnSocketDisconnected), null);
            }
        }
        
        public void BeginDisconnect(AsyncCallback callback, object state)
        {
            _socket.BeginDisconnect(true, callback, state);
        }
        #endregion

        #region Callback Methods
        public void EndConnect(IAsyncResult result)
        {
            _socket.EndConnect(result);
            InvokeConnected();
        }

        private void OnSocketDisconnected(IAsyncResult result)
        {
            _socket.EndDisconnect(result);
            InvokeDisconnected();
        }

        public void EndDisconnect(IAsyncResult result)
        {
            _socket.EndDisconnect(result);
        }
        #endregion

        #region Miscellaneous
        protected byte[] GetPortBytes()
        {
            return new byte[2] { (byte)(_targetEndPoint.Port / 256), (byte)(_targetEndPoint.Port % 256) };
        }

        protected byte[] GetAddressBytes(IPAddress address)
        {
            long value = IPAddress.HostToNetworkOrder(address.Address);
            byte[] ret = new byte[4];
            ret[0] = (byte)(value % 256);
            ret[1] = (byte)((value / 256) % 256);
            ret[2] = (byte)((value / 65536) % 256);
            ret[3] = (byte)(value / 16777216);
            return ret;
        }
        #endregion

        #region Event Invokers
        protected void InvokeConnected()
        {
            OnConnected?.Invoke(this, new EventArgs());
            InvokeStatus("Connected.");
        }

        protected void InvokeDisconnected()
        {
            OnDisconnected?.Invoke(this, new EventArgs());
            InvokeStatus("Disconnected.");
        }

        protected void InvokeStatus(string status)
        {
            OnStatusChanged?.Invoke(this, new StatusChangedEventArgs(status));
        }
        #endregion

        #region IDisposable Support
        public void Dispose()
        {
            if (!_disposed)
            {
                _socket.Dispose();

                _disposed = true;
            }
        }
        #endregion
    }
}