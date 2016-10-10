namespace ProxySocket.Enumerations
{
    public enum Socks4Response
    {
        RequestGranted = 0x5A,
        RequestRejected = 0x5B,
        RequestFailed = 0x5C,
        UserUnknown = 0x5D
    }
}