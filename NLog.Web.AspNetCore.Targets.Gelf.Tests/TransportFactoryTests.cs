using System.Net;
using Xunit;
using NSubstitute;
using System;

namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    public class TransportFactoryTests
    {
        private IDns _dns;

        public TransportFactoryTests()
        {
            _dns = Substitute.For<IDns>();
        }

        [Fact]
        public void ShouldeCreateTransportCreateUdpTransportWhenEndpointHasValidUdpEndpoint()
        {
            const int expectedChunkSize = 1500;
            var endpointUri = new Uri("udp://graylog.host.com:12201/");

            var factory = new TransportFactory(_dns);

            var target = Substitute.For<IGelfTarget>();
            target.EndpointUri.Returns(endpointUri);
            target.MaxUdpChunkSize.Returns(expectedChunkSize);

            _dns.GetHostAddresses(endpointUri.Host).Returns(new IPAddress[]{ IPAddress.Parse("127.0.0.1"), });

            // Act
            var transport = factory.CreateTransport(target);

            Assert.IsType<UdpTransport>(transport);

            var udpTransport = transport as UdpTransport;
            Assert.Equal(expectedChunkSize, udpTransport.MaxChunkSize);
            _dns.Received(1).GetHostAddresses(endpointUri.Host);
        }

        [Fact]
        public void ShouldeCreateTransportNotCreateUdpTransportWhenEndpointNotAValidUdpEndpoint()
        {
            var endpointUri = new Uri("http://graylog.host.com:12201/");

            var factory = new TransportFactory(_dns);

            var target = Substitute.For<IGelfTarget>();
            target.EndpointUri.Returns(endpointUri);

            // Act
            var transport = factory.CreateTransport(target);

            Assert.Null(transport);
            _dns.DidNotReceiveWithAnyArgs().GetHostAddresses(null);
        }

        [Fact]
        public void ShouldeCreateTransportNotCreateAnyTransportWhenEndpointIsNull()
        {
            var factory = new TransportFactory(_dns);

            var target = Substitute.For<IGelfTarget>();
            target.EndpointUri.Returns((Uri)null);

            // Act
            var transport = factory.CreateTransport(target);

            Assert.Null(transport);
            _dns.DidNotReceiveWithAnyArgs().GetHostAddresses(null);
        }
    }
}