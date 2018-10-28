using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using NLog.Common;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal class UdpTransport : ITransport
    {
        // Limitation from GrayLog2
        private const int MaxNumberOfChunksAllowed = 128;

        internal const int MinUdpDatagramSize = 576;
        internal const int MaxUdpDatagramSize = 8192;
        internal const int DefaultUdpDatagramSize = 1500;

        private readonly Random _messageIdGenerator = new Random();
        private readonly IUdpClient _udpClient;

        private bool _disposed;

        internal UdpTransport(IUdpClient udpClient, int maxChunkSize)
        {
            if (udpClient == null) throw new ArgumentNullException(nameof(udpClient));
            if (maxChunkSize < MinUdpDatagramSize || maxChunkSize > MaxUdpDatagramSize) throw new ArgumentOutOfRangeException(nameof(maxChunkSize), $"MaxChunkSize must be in the interval: [{MinUdpDatagramSize}, {MaxUdpDatagramSize}]");

            _udpClient = udpClient;
            _udpClient.DontFragment = true;

            MaxChunkSize = maxChunkSize;
        }

        public UdpTransport(IPEndPoint ipEndpoint, int maxChunkSize)
            : this(new UdpClientWrapper(ipEndpoint), maxChunkSize)
        {
        }

        internal int MaxChunkSize { get; }
        
        /// <summary>
        /// Sends a message to GrayLog2 server
        /// </summary>
        /// <param name="message">Message (in JSON) to log</param>
        public void Send(JObject jObject)
        {
            if (jObject == null) throw new ArgumentNullException(nameof(jObject));

            var message = jObject.ToString(Formatting.None, null);

            var compressedMessage = CompressMessage(message);

            if (compressedMessage.Length > MaxChunkSize)
            {
                var messageSize = MaxChunkSize - 12; // Chunk also contains 12 byte prefix.

                // Our compressed message is too big to fit in a single datagram. Need to chunk...
                // https://github.com/Graylog2/graylog2-docs/wiki/GELF "Chunked GELF"
                var numberOfChunksRequired = compressedMessage.Length / messageSize + 1;

                if (numberOfChunksRequired <= MaxNumberOfChunksAllowed)
                { 
                    var messageId = GenerateMessageId();

                    for (var i = 0; i < numberOfChunksRequired; i++)
                    {
                        var skip = i * messageSize;
                        var messageChunkHeader = ConstructChunkHeader(messageId, i, numberOfChunksRequired);
                        var messageChunkData = compressedMessage.Skip(skip).Take(messageSize).ToArray();

                        var messageChunkFull = new byte[messageChunkHeader.Length + messageChunkData.Length];
                        messageChunkHeader.CopyTo(messageChunkFull, 0);
                        messageChunkData.CopyTo(messageChunkFull, messageChunkHeader.Length);

                        _udpClient.SendAsync(messageChunkFull, messageChunkFull.Length);
                    }
                }
                else
                {
                    InternalLogger.Debug(() => $"Unable to transport datagram, chunksize exceeded the limit ({MaxNumberOfChunksAllowed})!");
                    return;
                }
            }
            else
            {
                _udpClient.SendAsync(compressedMessage, compressedMessage.Length);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _udpClient?.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// Chunk header structure is:
        /// - Chunked GELF ID: 0x1e 0x0f (identifying this message as a chunked GELF message)
        /// - Message ID: 8 bytes (Must be the same for every chunk of this message. Identifying the whole message itself and is used to reassemble the chunks later.)
        /// - Sequence Number: 1 byte (The sequence number of this chunk)
        /// - Total Number: 1 byte (How many chunks does this message consist of in total)
        /// </summary>
        /// <param name="messageId">Unique identifier of the whole message (not just this chunk)</param>
        /// <param name="chunkSequenceNumber">Sequence number of this chunk</param>
        /// <param name="chunkCount">Total number of chunks whole message consists of</param>
        /// <returns>Chunk header in bytes</returns>
        internal static byte[] ConstructChunkHeader(byte[] messageId, int chunkSequenceNumber, int chunkCount)
        {
            var b = new byte[12];

            b[0] = 0x1e;
            b[1] = 0x0f;
            messageId.CopyTo(b, 2);
            b[10] = (byte)chunkSequenceNumber;
            b[11] = (byte)chunkCount;

            return b;
        }

        /// <summary>
        /// Compresses the given message using GZip algorithm
        /// </summary>
        /// <param name="message">Message to be compressed</param>
        /// <returns>Compressed message in bytes</returns>
        internal static byte[] CompressMessage(in string message)
        {
            using (var compressedMessageStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedMessageStream, CompressionMode.Compress))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                gzipStream.Write(messageBytes, 0, messageBytes.Length);
                gzipStream.Flush();
                return compressedMessageStream.ToArray();
            }
        }

        internal byte[] GenerateMessageId()
        {
            var bytes = new byte[8];

            lock (_messageIdGenerator)
                _messageIdGenerator.NextBytes(bytes);

            return bytes;
        }
    }
}