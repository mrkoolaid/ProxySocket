using System;
using System.Net;
using System.Text;
using ProxySocket.Enumerations;
using ProxySocket.Objects;

namespace ProxySocket.Proxy
{
    public delegate void RequestAcceptedEventHandler(object sender, EventArgs e);

    public sealed class Socks4Proxy : BaseProxy
    {
        #region Events
        public event RequestAcceptedEventHandler OnRequestAccepted;
        #endregion

        #region Constructors
        public Socks4Proxy(string proxyHost, int proxyPort, string targetHost, int targetPort) : base(proxyHost, proxyPort, targetHost, targetPort) { }

        public Socks4Proxy(IPAddress proxyAddress, int proxyPort, IPAddress targetAddress, int targetPort) : base(proxyAddress, proxyPort, targetAddress, targetPort) { }

        public Socks4Proxy(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint) : base(proxyEndPoint, targetEndPoint) { }
        #endregion

        #region Connection Methods
        public void AutoConnect()
        {
            Connect();

            SendRequest();
        }

        public void AutoConnectAsync()
        {
            BeginConnect(new AsyncCallback(OnConnected), null);
        }
        #endregion

        #region Request Methods
        public void SendRequest()
        {
            Packet packet = BuildRequest();

            SendPacket(packet);
            ReceivePacket(8);
            CheckResponse();
        }

        public void BeginSendRequest(AsyncCallback callback, object state)
        {
            Packet packet = BuildRequest();
            SendPacketAsync(packet, callback, state);
        }

        public void EndSendRequest(IAsyncResult result)
        {
            EndSend("Request sent.", result);
        }
        #endregion

        #region Response Methods
        public void BeginReceiveResponse(AsyncCallback callback, object state)
        {
            ReceivePacketAsync(System.Net.Sockets.SocketFlags.None, callback, state, 8);
        }

        public void EndReceiveResponse(IAsyncResult result)
        {
            _socket.EndReceive(result);

            CheckResponse();
        }
        #endregion

        #region Callback Methods
        private new void OnConnected(IAsyncResult result)
        {
            _socket.EndConnect(result);

            BeginSendRequest(new AsyncCallback(OnRequestSent), null);
        }

        private void OnRequestSent(IAsyncResult result)
        {
            EndSend("Request sent.", result);

            BeginReceiveResponse(new AsyncCallback(OnResponseReceived), null);
        }

        private void OnResponseReceived(IAsyncResult result)
        {
            _socket.EndReceive(result);
        }
        #endregion

        #region Builder Methods
        private Packet BuildRequest(string username = "")
        {
            byte[] portBytes = GetPortBytes();
            byte[] addressBytes = GetAddressBytes(_targetEndPoint.Address);
            byte[] userBytes = Encoding.ASCII.GetBytes(username);

            Packet packet = new Packet();
            packet.AppendData((byte)SocksVersion.SOCKS4);
            packet.AppendData(0x01);
            packet.AppendData(portBytes);
            packet.AppendData(addressBytes);
            packet.AppendData(userBytes);
            packet.AppendData(0x00);

            return packet;
        }
        #endregion

        #region Checker Methods
        private void CheckResponse()
        {
            Socks4Response response = (Socks4Response)buffer[1];

            if (response == Socks4Response.RequestGranted)
            {
                _connected = true;
                InvokeRequestAccepted();
            }
            else
            {
                if (!_disposed)
                    Dispose();
            }

            InvokeStatus($"Response: {response.ToString()}.\r\nConnected: {_connected}");
        }
        #endregion

        #region Event Invokers
        private void InvokeRequestAccepted()
        {
            OnRequestAccepted?.Invoke(this, new EventArgs());
        }
        #endregion
    }
}