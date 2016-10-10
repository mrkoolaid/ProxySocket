using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using ProxySocket.Objects;
using ProxySocket.Enumerations;

namespace ProxySocket.Proxy
{
    public delegate void MethodAcceptedEventHandler(object sender, EventArgs e);
    public delegate void CredentialsAcceptedEventHandler(object sender, EventArgs e);
    public delegate void HandshakeAcceptedEventHandler(object sender, EventArgs e);

    public sealed class Socks5Proxy : BaseProxy
    {
        #region Properties
        public bool Connected { get { return _connected; } }
        public Socks5Method NegotiatedMethod { get { return _negotiatedMethod; } }
        #endregion

        #region Events
        public event MethodAcceptedEventHandler OnMethodAccepted;
        public event CredentialsAcceptedEventHandler OnCredentialsAccepted;
        public event HandshakeAcceptedEventHandler OnHandshakeAccepted;
        #endregion

        #region Fields
        private bool _connected;
        private Socks5Method _negotiatedMethod;
        #endregion

        #region Constructors
        public Socks5Proxy(string proxyHost, int proxyPort, string targetHost, int targetPort) : base(proxyHost, proxyPort, targetHost, targetPort) { }

        public Socks5Proxy(IPAddress proxyAddress, int proxyPort, IPAddress targetAddress, int targetPort) : base(proxyAddress, proxyPort, targetAddress, targetPort) { }

        public Socks5Proxy(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint) : base(proxyEndPoint, targetEndPoint) { }
        #endregion

        #region Connection Methods
        public new void Connect()
        {
            base.Connect();
        }
        
        public void AutoConnect(params Socks5Method[] methods)
        {
            if (Array.Exists(methods, method => method == Socks5Method.Credentials) && (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)))
                throw new ArgumentException("No username or password provided.");

            base.Connect();
            Negotiate(methods);

            if (_negotiatedMethod == Socks5Method.Credentials)
                VerifyCredentials(Username, Password);

            Handshake();
        }

        public void AutoConnectAsync(params Socks5Method[] methods)
        {
            BeginConnect(new AsyncCallback(OnAutoConnected), methods);
        }

        private void OnAutoConnected(IAsyncResult result)
        {
            EndConnect(result);

            Socks5Method[] methods = (Socks5Method[])result.AsyncState;
            BeginSendNegotiation(new AsyncCallback(OnNegotiationSent), methods);
        }
        #endregion

        #region Negotiation Methods
        #region Outgoing
        public void Negotiate(params Socks5Method[] methods)
        {
            Packet packet = BuildNegotiationPacket(methods);
            SendPacket(packet);
            InvokeStatus("Sent negotiation.");

            ReceiveNegotiation();
        }

        public void BeginSendNegotiation(AsyncCallback callback, object state, params Socks5Method[] methods)
        {
            Packet packet = BuildNegotiationPacket(methods);
            SendPacketAsync(packet, callback, state);
        }

        public void EndSendNegotiation(IAsyncResult result)
        {
            EndSend("Sent negotiation.", result);
        }
        #endregion

        #region Incoming
        public void BeginReceiveNegotiation(AsyncCallback callback, object state = null)
        {
            ReceivePacketAsync(2, SocketFlags.None, callback, state);
        }

        public void EndReceiveNegotiation(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckNegotiation();
        }

        private void ReceiveNegotiation()
        {
            buffer = new byte[2];
            _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

            CheckNegotiation();
        }
        #endregion
        #endregion

        #region Login Methods
        #region Outgoing
        public void VerifyCredentials(string username, string password)
        {
            Packet packet = BuildCredentialsPacket();
            SendPacket(packet);
            InvokeStatus("Sent credentials.");

            ReceiveCredentials();
        }

        public void BeginSendCredentials(AsyncCallback callback)
        {
            Packet packet = BuildCredentialsPacket();
            SendPacketAsync(packet, callback, null);
        }

        public void EndSendCredentials(IAsyncResult result)
        {
            EndSend("Credentials sent.", result);
        }
        #endregion

        #region Incoming
        public void BeginReceiveCredentials(AsyncCallback callback, object state = null)
        {
            ReceivePacketAsync(2, SocketFlags.None, callback, state);
        }

        public void EndReceiveCredentials(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckCredentials();
        }

        private void ReceiveCredentials()
        {
            buffer = new byte[2];
            int size = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

            CheckCredentials();
        }
        #endregion
        #endregion

        #region Handshake Methods
        #region Outgoing
        public void Handshake()
        {
            Packet packet = BuildRequestDetails();
            SendPacket(packet);
            InvokeStatus("Sent handshake.");

            ReceiveRequestDetails();
        }

        public void BeginSendRequestDetails(AsyncCallback callback, object state)
        {
            Packet packet = BuildRequestDetails();
            SendPacketAsync(packet, callback, state);
        }

        public void EndSendRequestDetails(IAsyncResult result)
        {
            EndSend("Request details sent.", result);
        }
        #endregion

        #region Incoming
        public void BeginReceiveRequestDetails(AsyncCallback callback, object state = null)
        {
            ReceivePacketAsync(18, SocketFlags.None, callback, state);
        }

        public void EndReceiveRequestDetails(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckRequestDetails();
        }

        private void ReceiveRequestDetails()
        {
            buffer = new byte[18];
            _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

            CheckRequestDetails();
        }
        #endregion
        #endregion

        #region Callback Methods
        private void OnNegotiationSent(IAsyncResult result)
        {
            EndSend("Sent negotiation.", result);

            BeginReceiveNegotiation(new AsyncCallback(OnNegotiationReceived));
        }

        private void OnNegotiationReceived(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckNegotiation();

            if (_negotiatedMethod == Socks5Method.Credentials)
                BeginSendCredentials(new AsyncCallback(OnCredentialsSent));
            else
            {
                BeginSendRequestDetails(new AsyncCallback(OnRequestDetailsSent), null);
            }
        }

        private void OnCredentialsSent(IAsyncResult result)
        {
            EndSend("Credentials sent.", result);

            BeginReceiveCredentials(new AsyncCallback(OnCredentialsReceived));
        }

        private void OnCredentialsReceived(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckCredentials();

            BeginSendRequestDetails(new AsyncCallback(OnRequestDetailsSent), null);
        }

        private void OnRequestDetailsSent(IAsyncResult result)
        {
            EndSend("Request details sent.", result);

            BeginReceiveRequestDetails(new AsyncCallback(OnRequestDetailsReceived));
        }

        private void OnRequestDetailsReceived(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckRequestDetails();
        }
        #endregion

        #region Checking Methods
        private void CheckNegotiation()
        {
            CheckVersion(buffer[0]);

            _negotiatedMethod = (Socks5Method)buffer[1];

            if (_negotiatedMethod == Socks5Method.NoneAcceptable)
            {
                Disconnect();
                throw new Exception("Could not come to an agreement with the proxy.");
            }

            InvokeMethodAccepted();
        }

        private void CheckCredentials()
        {
            CheckVersion(buffer[0]);

            if (buffer[1] != 0x00)
            {
                Disconnect();
                throw new Exception("Credentials not accepted.");
            }

            InvokeCredentialsAccepted();
        }

        private void CheckRequestDetails()
        {
            CheckVersion(buffer[0]);

            Socks5Response response = (Socks5Response)buffer[1];

            if (response == Socks5Response.Succeeded)
            {
                _connected = true;
                InvokeHandshakeAccepted();
            }
            else
                Disconnect();

            InvokeStatus($"Response: {response.ToString()}.\r\nConnected: {_connected}");
        }
        #endregion

        #region Building Methods
        private Packet BuildNegotiationPacket(Socks5Method[] methods)
        {
            Packet packet = new Packet();
            packet.AppendData((byte)SocksVersion.SOCKS5);
            packet.AppendData((byte)methods.Length);

            foreach (Socks5Method method in methods)
                packet.AppendData((byte)method);

            return packet;
        }

        private Packet BuildCredentialsPacket()
        {
            byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(Password);

            Packet packet = new Packet();
            packet.AppendData((byte)SocksVersion.SOCKS5);
            packet.AppendData((byte)usernameBytes.Length);
            packet.AppendData(usernameBytes);
            packet.AppendData((byte)passwordBytes.Length);
            packet.AppendData(passwordBytes);

            return packet;
        }

        private Packet BuildRequestDetails()
        {
            byte[] addressBytes = _targetEndPoint.Address.GetAddressBytes();
            byte[] portBytes = new byte[2] { (byte)(_targetEndPoint.Port / 256), (byte)(_targetEndPoint.Port % 256) };

            Packet packet = new Packet();
            packet.AppendData((byte)SocksVersion.SOCKS5);
            packet.AppendData(0x01);
            packet.AppendData(0x00);

            if (_targetEndPoint.AddressFamily == AddressFamily.InterNetwork)
                packet.AppendData(0x01);
            else if (_targetEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                packet.AppendData(0x04);
            else
                packet.AppendData(0x03);

            packet.AppendData(addressBytes);
            packet.AppendData(portBytes);

            return packet;
        }
        #endregion

        #region Miscellaneous Methods
        private void CheckVersion(byte version)
        {
            if (version != (byte)SocksVersion.SOCKS5)
            {
                _socket.Dispose();
                throw new ProtocolViolationException("Proxy does not support the socks 5 protocol. Attempt a socks 4 approach.");
            }
        }

        private void EndSend(string status, IAsyncResult result)
        {
            _socket.EndSend(result);
            InvokeStatus(status);
        }
        #endregion

        #region Event Invokers
        public void InvokeMethodAccepted()
        {
            OnMethodAccepted?.Invoke(this, new EventArgs());
            InvokeStatus($"Negotiated method: {_negotiatedMethod.ToString()}.");
        }

        public void InvokeCredentialsAccepted()
        {
            OnCredentialsAccepted?.Invoke(this, new EventArgs());
            InvokeStatus("Credentials accepted.");
        }

        public void InvokeHandshakeAccepted()
        {
            OnHandshakeAccepted?.Invoke(this, new EventArgs());
        }
        #endregion
    }
}