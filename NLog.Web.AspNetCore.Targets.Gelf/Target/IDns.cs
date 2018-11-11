using System.Net;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface IDns
    {
        IPAddress[] GetHostAddresses(string hostNameOrAddress);

        string GetHostName();
    }
}