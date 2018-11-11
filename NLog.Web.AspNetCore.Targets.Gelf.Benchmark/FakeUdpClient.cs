using System.Threading.Tasks;

namespace NLog.Web.AspNetCore.Targets.Gelf.Benchmark
{
    internal sealed class FakeUdpClient : IUdpClient
    {
        public bool DontFragment { get; set; }

        public void Dispose()
        {
            // Method intentionally left empty.
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            return Task.FromResult(bytes);
        }
    }
}