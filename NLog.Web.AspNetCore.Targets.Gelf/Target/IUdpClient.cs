using System;
using System.Threading.Tasks;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface IUdpClient : IDisposable
    {
        bool DontFragment { get; set; }

        Task<int> SendAsync(byte[] datagram, int bytes);
    }
}