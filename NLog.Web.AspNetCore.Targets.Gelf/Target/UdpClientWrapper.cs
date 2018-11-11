using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal sealed class UdpClientWrapper : IUdpClient
    {
        private readonly UdpClient _client;
        private readonly IPEndPoint _endpoint;

        public UdpClientWrapper(IPEndPoint endpoint)
        {
            _client = new UdpClient();
            _endpoint = endpoint;
        }

        public bool DontFragment
        {
            get => _client.DontFragment;
            set => _client.DontFragment = value;
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            return _client.SendAsync(datagram, bytes, _endpoint);
        }

        public void Dispose() => _client.Dispose();
    }
}