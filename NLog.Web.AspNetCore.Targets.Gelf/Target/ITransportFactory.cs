using System.Collections.Generic;
using System.Net;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface ITransportFactory
    {
        ITransport CreateTransport(IGelfTarget target);
    }
}