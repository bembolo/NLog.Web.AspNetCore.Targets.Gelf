using System.Net;
using Newtonsoft.Json.Linq;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal sealed class HttpTransport : ITransport
    {
        public HttpTransport(IPEndPoint endpoint)
        {
            throw new System.NotImplementedException();
        }

        public void Send(JObject message)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}