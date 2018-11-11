using System.Net;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal class DnsWrapper : IDns
    {
        public IPAddress[] GetHostAddresses(string hostNameOrAddress)
        {
            return Dns.GetHostAddressesAsync(hostNameOrAddress).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public string GetHostName()
        {
            return Dns.GetHostName();
        }
    }
}