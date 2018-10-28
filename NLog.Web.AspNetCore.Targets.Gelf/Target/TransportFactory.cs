using NLog.Common;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal class TransportFactory : ITransportFactory
    {
        private readonly IDns _dns;

        public TransportFactory(IDns dns)
        {
            _dns = dns;
        }
        public ITransport CreateTransport(IGelfTarget target)
        {
            ITransport transport = null;

            if (target.EndpointUri == null)
            {
                InternalLogger.Warn($"Unable to create transport as {nameof(GelfTarget)} has no {nameof(GelfTarget.Endpoint)}!");
            }
            else
            {
                transport = CreateUdpTransport(target);

                if (transport == null)
                    InternalLogger.Warn("No transport could be created for the given endpoint");
            }

            return transport;
        }

        private ITransport CreateUdpTransport(IGelfTarget target)
        {
            ITransport result = null;

            if (target.EndpointUri.Scheme.ToUpper() == "UDP")
            {
                IPEndPoint ipEndpoint = GetIpEndpoint(target.EndpointUri);

                if (ipEndpoint != null)
                    result = new UdpTransport(ipEndpoint, target.MaxUdpChunkSize);
                else
                    InternalLogger.Warn($"Unable to determine IPv4 address of host: {target.EndpointUri.Host}");
            }

            return result;
        }

        private IPEndPoint GetIpEndpoint(Uri endpointUri)
        {
            return new IPEndPoint(
                    _dns.GetHostAddresses(endpointUri.Host)
                        .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork),
                    endpointUri.Port);
        }
    }
}