using Newtonsoft.Json.Linq;
using System;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface ITransport : IDisposable
    {
        void Send(JObject message);
    }
}