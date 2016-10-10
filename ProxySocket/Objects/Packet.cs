using System.Collections.Generic;

namespace ProxySocket.Objects
{
    internal sealed class Packet
    {
        public byte[] Buffer { get { return _data.ToArray(); } }
        public List<byte> Data { get { return _data; } }

        private List<byte> _data;

        public Packet()
        {
            _data = new List<byte>();
        }

        public void AppendData(params byte[] data)
        {
            _data.AddRange(data);
        }
    }
}