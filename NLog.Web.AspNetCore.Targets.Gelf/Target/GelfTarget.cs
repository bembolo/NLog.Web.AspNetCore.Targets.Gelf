using NLog.Targets;
using Newtonsoft.Json;
using NLog.Config;
using System.Collections.Generic;
using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using NLog.Common;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    [Target("Gelf")]
    public class GelfTarget : TargetWithLayout, IGelfTarget
    {
        private readonly IConverter _converter;
        private Lazy<ITransport> _lazyITransport;
        private string _facility;
        private int _maxUdpChunkSize = UdpTransport.DefaultUdpDatagramSize;
        private bool _disposed;
        private Uri _endpointUri;
        private int _maxNestedExceptionsDepth = GelfConverter.DefaultMaxNestedExceptionsDepth;

        public GelfTarget()
            : this(new TransportFactory(new DnsWrapper()), new GelfConverter(new DnsWrapper()))
        {
        }

        internal GelfTarget(ITransportFactory transportFactory, IConverter converter)
        {
            _converter = converter;
            _converter.Target = this;
            Parameters = new List<GelfParameterInfo>();

            _lazyITransport = new Lazy<ITransport>(() => transportFactory.CreateTransport(this));
        }

        Uri IGelfTarget.EndpointUri => _endpointUri;

        public bool SendLastFormatParameter { get; set; }

        [Required]
        public string Endpoint
        {
            get { return _endpointUri?.OriginalString; }
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

        [ArrayParameter(typeof(GelfParameterInfo), "parameter")]
        public IList<GelfParameterInfo> Parameters { get; private set; }

        public string Facility
        {
            get { return _facility; }
            set { _facility = value != null ? Environment.ExpandEnvironmentVariables(value) : null; }
        }

        public int MaxUdpChunkSize
        {
            get { return _maxUdpChunkSize; }
            set
            {
                _maxUdpChunkSize = Math.Max(UdpTransport.MinUdpDatagramSize, Math.Min(value, UdpTransport.MaxUdpDatagramSize));
                InternalLogger.Info(() => $"{nameof(MaxUdpChunkSize)} is set to {MaxUdpChunkSize}");
            }
        }

        public int MaxNestedExceptionsDepth
        {
            get { return _maxNestedExceptionsDepth; }
            set
            {
                _maxNestedExceptionsDepth = Math.Max(0, value);
                InternalLogger.Info(() => $"{nameof(MaxNestedExceptionsDepth)} is set to {MaxNestedExceptionsDepth}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_lazyITransport.IsValueCreated)
                {
                    _lazyITransport.Value?.Dispose();
                }

                _disposed = true;
            }
        }

        internal void InternalWrite(LogEventInfo logEvent)
        {
            Write(logEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (_lazyITransport.Value != null)
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
                    
                    _lazyITransport.Value.Send(gelfObject);
                }
            }
        }
    }
}