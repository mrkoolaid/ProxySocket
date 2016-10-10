using System;
using ProxySocket.Proxy;
using ProxySocket.EventArguments;
using ProxySocket.Enumerations;

namespace ProxySocket_test
{
    class Program
    {
        static void Main(string[] args)
        {
            //socks 4:
            //180.183.138.89:1080

            //socks 5:
            //189.218.41.64:24732
            //45.64.109.2:1080
            Socks4Proxy proxy = new Socks4Proxy("182.253.44.117", 1080, "google.com", 80);
            proxy.OnStatusChanged += OnStatusChanged;
            proxy.OnRequestAccepted += Proxy_OnRequestAccepted;
            //proxy.BeginConnect(new AsyncCallback(OnConnected), proxy);
            proxy.AutoConnectAsync();
            //proxy.AutoConnectAsync(Socks5Method.NoAuthentication);
            Console.ReadLine();
        }

        private static void Proxy_OnRequestAccepted(object sender, EventArgs e)
        {
            BaseProxy proxy = (BaseProxy)sender;
            proxy.Disconnect();
        }

        #region Callback Methods
        private static void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            Console.WriteLine(e.Status);
        }

        private static void OnConnected(IAsyncResult result)
        {
            Socks5Proxy proxy = (Socks5Proxy)result.AsyncState;
            proxy.EndConnect(result);

            proxy.BeginSendNegotiation(new AsyncCallback(OnNegotiationSent), proxy, Socks5Method.NoAuthentication);
        }

        private static void OnNegotiationSent(IAsyncResult result)
        {
            Socks5Proxy proxy = (Socks5Proxy)result.AsyncState;
            proxy.EndSendNegotiation(result);

            proxy.BeginReceiveNegotiation(new AsyncCallback(OnNegotiationReceived), proxy);
        }

        private static void OnNegotiationReceived(IAsyncResult result)
        {
            Socks5Proxy proxy = (Socks5Proxy)result.AsyncState;
            proxy.EndReceiveNegotiation(result);

            proxy.BeginSendRequestDetails(new AsyncCallback(OnRequestDetailsSent), proxy);
        }

        private static void OnRequestDetailsSent(IAsyncResult result)
        {
            Socks5Proxy proxy = (Socks5Proxy)result.AsyncState;
            proxy.EndSendRequestDetails(result);

            proxy.BeginReceiveRequestDetails(new AsyncCallback(OnRequestDetailsReceived), proxy);
        }

        private static void OnRequestDetailsReceived(IAsyncResult result)
        {
            Socks5Proxy proxy = (Socks5Proxy)result.AsyncState;
            proxy.EndReceiveRequestDetails(result);

            proxy.Disconnect();
        }
        #endregion
    }
}