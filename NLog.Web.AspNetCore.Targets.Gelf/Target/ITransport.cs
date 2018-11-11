using System;
using Newtonsoft.Json.Linq;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface ITransport : IDisposable
    {
        void Send(JObject message);
    }
}