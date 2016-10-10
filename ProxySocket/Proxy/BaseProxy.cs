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

    public abstract class BaseProxy
    {
        #region Properties
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
        internal Socket _socket;
        internal IPEndPoint _proxyEndPoint;
        internal IPEndPoint _targetEndPoint;
        internal byte[] buffer;
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

        public void EndConnect(IAsyncResult result)
        {
            _socket.EndConnect(result);
            InvokeConnected();
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
        internal void SendPacket(Packet packet)
        {
            byte[] buffer = packet.Buffer;
            _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        internal void SendPacketAsync(Packet packet, AsyncCallback callback, object state)
        {
            byte[] buffer = packet.Buffer;
            _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, callback, state);
        }

        internal void ReceivePacketAsync(int size, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            buffer = new byte[size];
            _socket.BeginReceive(buffer, 0, size, socketFlags, callback, state);
        }
        #endregion

        #region Disconnection Methods
        public void Disconnect()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Disconnect(true);
            InvokeDisconnected();
        }

        public void DisconnectAsync()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.BeginDisconnect(true, new AsyncCallback(OnSocketDisconnected), null);
        }

        private void OnSocketDisconnected(IAsyncResult result)
        {
            _socket.EndDisconnect(result);
            InvokeDisconnected();
        }

        public void BeginDisconnect(AsyncCallback callback, object state)
        {
            _socket.BeginDisconnect(true, callback, state);
        }

        public void EndDisconnect(IAsyncResult result)
        {
            _socket.EndDisconnect(result);
        }
        #endregion

        #region Event Invokers
        internal void InvokeConnected()
        {
            OnConnected?.Invoke(this, new EventArgs());
            InvokeStatus("Connected.");
        }

        internal void InvokeDisconnected()
        {
            OnDisconnected?.Invoke(this, new EventArgs());
            InvokeStatus("Disconnected.");
        }

        internal void InvokeStatus(string status)
        {
            OnStatusChanged?.Invoke(this, new StatusChangedEventArgs(status));
        }
        #endregion
    }
}