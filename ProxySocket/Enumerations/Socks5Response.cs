namespace ProxySocket.Enumerations
{
    public enum Socks5Response
    {
        Succeeded = 0x00,
        GeneralFailure = 0x01,
        NotAllowed = 0x02,
        NetworkUnreachable = 0x03,
        HostUnreachable = 0x04,
        ConnectionRefused = 0x05,
        TTLExpired = 0x06,
        CommandNotSupported = 0x07,
        AddressTypeNotSupported = 0x08,
    }
}