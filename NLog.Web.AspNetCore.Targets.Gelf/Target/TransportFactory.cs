using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NLog.Common;

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
                transport = new[]
                {
                    CreateUdpTransport(target),
                    CreateHttpTransport(target)
                }.FirstOrDefault(t => t != null);

                if (transport == null)
                    InternalLogger.Warn("No transport could be created for the given endpoint");
            }

            return transport;
        }

        private ITransport CreateHttpTransport(IGelfTarget target)
        {
            return CreateTransport(target, ipEndpoint => new HttpTransport(ipEndpoint), "http", "https");
        }

        private ITransport CreateUdpTransport(IGelfTarget target)
        {
            return CreateTransport(target, ipEndpoint => new UdpTransport(ipEndpoint, target.MaxUdpChunkSize), "udp");
        }

        private ITransport CreateTransport(IGelfTarget target, Func<IPEndPoint, ITransport> transportFactory, params string[] matchingUriSchemes)
        {
            ITransport result = null;
            var scheme = target.EndpointUri.Scheme.ToUpperInvariant();
            matchingUriSchemes = matchingUriSchemes.Select(s => s.ToUpperInvariant()).ToArray();

            if (matchingUriSchemes.Contains(scheme))
            {
                IPEndPoint ipEndpoint = GetIpEndpoint(target.EndpointUri);

                if (ipEndpoint != null)
                    result = transportFactory(ipEndpoint);
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