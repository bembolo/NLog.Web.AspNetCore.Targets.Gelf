using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    /// <summary>
    /// NLog target that sends GELF 1.1 messages to Graylog2.
    /// </summary>
    [Target("Gelf")]
    public class GelfTarget : TargetWithLayout, IGelfTarget
    {
        private readonly ITransportFactory _transportFactory;
        private readonly IConverter _converter;
        private ITransport _transport;
        private string _facility;
        private int _maxUdpChunkSize = UdpTransport.DefaultUdpDatagramSize;
        private bool _disposed;
        private Uri _endpointUri;
        private int _maxNestedExceptionsDepth = GelfConverter.DefaultMaxNestedExceptionsDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="GelfTarget"/> class.
        /// </summary>
        public GelfTarget()
            : this(new TransportFactory(new DnsWrapper()), new GelfConverter(new DnsWrapper()))
        {
        }

        internal GelfTarget(ITransportFactory transportFactory, IConverter converter)
        {
            _transportFactory = transportFactory;
            _converter = converter;

            Parameters = new List<GelfParameterInfo>();
        }

        /// <summary>
        /// Gets the endpoint set by the <see cref="Endpoint"/> property as an <see cref="Uri"/> instance.
        /// </summary>
        public Uri EndpointUri => _endpointUri;

        /// <summary>
        /// Gets or sets a value indicating whether last parameter of message format should be sent to graylog as separate field per property.
        /// </summary>
        public bool SendLastFormatParameter { get; set; }

        /// <summary>
        /// Gets or sets the endpoint uri pointing to the Graylog2 input in the format udp://{IP or host name}:{port}. Note: support is currently only for UDP transport and HTTP application protocol
        /// </summary>
        [Required]
        public string Endpoint
        {
            get => _endpointUri?.OriginalString;
            set
            {
                if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
                {
                    Environment.ExpandEnvironmentVariables(value);
                    _endpointUri = uri;
                }
                else
                {
                    InternalLogger.Info(() => "Endpoint is not a valid Uri!");
                }
            }
        }

        /// <summary>
        /// Gets the optional parameters to be included in the GELF message.
        /// </summary>
        [ArrayParameter(typeof(GelfParameterInfo), "parameter")]
        public IList<GelfParameterInfo> Parameters { get; private set; }

        /// <summary>
        /// Gets or sets the facility of the GELF message.
        /// </summary>
        public string Facility
        {
            get { return _facility; }
            set { _facility = value != null ? Environment.ExpandEnvironmentVariables(value) : null; }
        }

        /// <summary>
        /// Gets or sets the maximum number of bytes of the UDP datagram chunk. Note that valu is coerced to [576, 8192].
        /// </summary>
        public int MaxUdpChunkSize
        {
            get => _maxUdpChunkSize;
            set
            {
                _maxUdpChunkSize = Math.Max(UdpTransport.MinUdpDatagramSize, Math.Min(value, UdpTransport.MaxUdpDatagramSize));
                InternalLogger.Info(() => $"{nameof(MaxUdpChunkSize)} is set to {MaxUdpChunkSize}");
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of nested exception to be logged. Default value is 10.
        /// </summary>
        public int MaxNestedExceptionsDepth
        {
            get => _maxNestedExceptionsDepth;
            set
            {
                _maxNestedExceptionsDepth = Math.Max(0, value);
                InternalLogger.Info(() => $"{nameof(MaxNestedExceptionsDepth)} is set to {MaxNestedExceptionsDepth}");
            }
        }

        /// <inheritdoc/>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            _converter.Target = this;
            _transport = _transportFactory.CreateTransport(this);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transport?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <inheritdoc/>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent != null && _transport != null)
            {
                foreach (var par in Parameters)
                {
                    if (!logEvent.Properties.ContainsKey(par.Name))
                    {
                        string stringValue = par.Layout.Render(logEvent);

                        logEvent.Properties.Add(par.Name, stringValue);
                    }
                }

                if (SendLastFormatParameter && logEvent.Parameters != null && logEvent.Parameters.Any())
                {
                    // PromoteObjectPropertiesMarker used as property name to indicate that the value should be treated as a object
                    // whose properties should be mapped to additional fields in graylog
                    logEvent.Properties.Add(ConverterConstants.PromoteObjectPropertiesMarker, logEvent.Parameters.Last());
                }

                var gelfObject = _converter.GetGelfObject(logEvent);

                if (gelfObject != null)
                {
                    WriteAsyncLogEvents();

                    _transport.Send(gelfObject);
                }
            }
        }
    }
}