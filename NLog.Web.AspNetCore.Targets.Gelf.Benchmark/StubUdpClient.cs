using System.Threading.Tasks;

namespace NLog.Web.AspNetCore.Targets.Gelf.Benchmark
{
    internal class StubUdpClient : IUdpClient
    {
        public bool DontFragment { get; set; }

        public void Dispose()
        {
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            return Task.FromResult(bytes);
        }
    }
}