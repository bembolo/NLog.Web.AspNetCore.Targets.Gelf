using System;
using System.Collections.Generic;
using System.IO;
using NSubstitute;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    public class UdpTransportTests
    {
        private IUdpClient _client;

        public UdpTransportTests()
        {
            _client = Substitute.For<IUdpClient>();
        }

        [Fact]
        public void ShouldConstructorCreateUdpTransport()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 12201);
            
            int expectedMaxChunkSize = 1500;

            // Act
            var transport = new UdpTransport(_client, expectedMaxChunkSize);

            Assert.Equal(expectedMaxChunkSize, transport.MaxChunkSize);
            _client.Received().DontFragment = true;
        }

        [Fact]
        public void ShouldConstructorThrowArgumentNullExceptionWhenUdpClientIsNull()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 12201);
            
            int expectedMaxChunkSize = 1500;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => new UdpTransport((IUdpClient)null, expectedMaxChunkSize));

            Assert.Equal("udpClient", ex.ParamName);
        }

        [Fact]
        public void ShouldCompressMessageCompressMessageToGZip()
        {
            const string expectedMessage = "lorem ipsum dolor sit amet";

            // Act
            var compressedMessage = UdpTransport.CompressMessage(expectedMessage);

            // Assert
            using (var ms = new MemoryStream(compressedMessage))
            using (var gZip = new GZipStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(gZip, Encoding.UTF8))
            {
                var message = sr.ReadToEnd();

                Assert.Equal(expectedMessage, message);
            }
        }

        [Fact]
        public void ShouldConstructChunkHeaderBytesConstructChunkHeader()
        {
            var bytes = new byte[] {0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08};

            // Act
            var chunkHeader = UdpTransport.ConstructChunkHeader(bytes, 1, 2);

            Assert.Equal(new byte[]{0x1e, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02}, chunkHeader);
        }

        [Fact]
        public void ShouldGenerateMessageIdGenerateEightBytes()
        {
            var transport = new UdpTransport(_client, 1500);

            // Act
            var messageId = transport.GenerateMessageId();

            Assert.Equal(8, messageId.Length);
        }

        [Fact]
        public void ShouldGenerateMessageIdGenerateThreadSafelyQuasiUniqueBytes()
        {
            var transport = new UdpTransport(_client, 1500);

            // Act
            var messageIds = Enumerable.Range(0, 10000).AsParallel().Select(_ => transport.GenerateMessageId()).ToList();

            Assert.Equal(10000, messageIds.Distinct(new ByteArrayEqualityComparer()).Count());
        }

        [Fact]
        public void ShouldDisposeDisposeUnderlyingUdpClient()
        {
            var client1 = Substitute.For<IUdpClient>();
            var client2 = Substitute.For<IUdpClient>();
            var transport = new UdpTransport(client1, 1500);
            var transport2 = new UdpTransport(client2, 1500);

            // Act
            transport.Dispose();

            transport2.Dispose();
            transport2.Dispose();

            // Assert
            client1.Received(1).Dispose();
            client2.Received(1).Dispose();
        }

        [Fact]
        public void ShouldSendSendDatagramChunkedWhenMessageLargerThanChunkSize()
        {
            const int chunkSize = 600;
            var udpClient = Substitute.For<IUdpClient>();

            var transport = new UdpTransport(_client, chunkSize);

            var value = string.Join(string.Empty, Enumerable.Range(0, 10_000).Select(i => i.ToString("X")));
            var jObject = new JObject(new JProperty("property", new JValue(value)));

            // Act
            transport.Send(jObject);

            _client.Received(34).SendAsync(Arg.Is<byte[]>(b => b.Length == 600), 600);
            _client.Received(1).SendAsync(Arg.Is<byte[]>(b => b.Length == 117), 117);
        }

        [Fact]
        public void ShouldSendNotSendDatagramWhenRequiredChunksExceedLimit()
        {
            const int chunkSize = 600;
            var udpClient = Substitute.For<IUdpClient>();

            var transport = new UdpTransport(_client, chunkSize);

            var value = string.Join(string.Empty, Enumerable.Range(0, 100_000).Select(i => i.ToString("X")));
            var jObject = new JObject(new JProperty("property", new JValue(value)));

            // Act
            transport.Send(jObject);

            _client.DidNotReceiveWithAnyArgs().SendAsync(null, 0);
        }

        [Fact]
        public void ShouldSendSendDatagram()
        {
            const int chunkSize = 1500;
            var udpClient = Substitute.For<IUdpClient>();

            var transport = new UdpTransport(_client, chunkSize);

            var jObject = new JObject();

            // Act
            transport.Send(jObject);

            _client.Received(1).SendAsync(Arg.Is<byte[]>(b => b.Length > 0), Arg.Any<int>());
        }

        private class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (ReferenceEquals(x, y)) return true;

                if ((x == null && y != null) || (x != null && y == null)) return false;

                if (x.Length != y.Length) return false;

                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i]) return false;
                }

                return true;
            }

            public int GetHashCode(byte[] obj)
            {
                return obj.Take(8).Sum(b => b);
            }
        }
    }
}