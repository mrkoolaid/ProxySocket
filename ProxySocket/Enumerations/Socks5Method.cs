namespace ProxySocket.Enumerations
{
    public enum Socks5Method
    {
        NoAuthentication = 0x00,
        GSSAPI = 0x01,
        Credentials = 0x02,
        NoneAcceptable = 0xFF
    }
}