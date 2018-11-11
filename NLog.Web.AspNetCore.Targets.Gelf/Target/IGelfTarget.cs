using System;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface IGelfTarget
    {
        Uri EndpointUri { get; }

        int MaxUdpChunkSize { get; }

        string Facility { get; }

        int MaxNestedExceptionsDepth { get; }
    }
}