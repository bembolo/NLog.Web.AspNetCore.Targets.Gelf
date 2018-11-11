using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CSharpx;
using NLog.Web.AspNetCore.Targets.Gelf.Tests;
using NSubstitute;

namespace NLog.Web.AspNetCore.Targets.Gelf.Benchmark
{
    [CoreJob]
    public sealed class GelfTargetBenchmark : IDisposable
    {
        private readonly IList<LogEventInfo> _logEvents;
        private readonly GelfConverter _converter;
        private readonly IUdpClient _udpClient;
        private readonly UdpTransport _udpTransport;
        private readonly ITransportFactory _transportFactory;
        private readonly TestGelfTarget _target;

        private bool _disposed;

        public GelfTargetBenchmark()
        {
            var random = new Random(1);
            _logEvents = new List<LogEventInfo>();

            PopulateLogEvents(random);

            _converter = new GelfConverter(Substitute.For<IDns>());
            _udpClient = new FakeUdpClient();
            _udpTransport = new UdpTransport(_udpClient, 1500);
            _transportFactory = Substitute.For<ITransportFactory>();

            _target = new TestGelfTarget(_transportFactory, _converter)
            {
                SendLastFormatParameter = true,
            };

            _transportFactory.CreateTransport(_target).Returns(_udpTransport);
        }

        [Benchmark]
        public void VersionTwoPointZero()
        {
            foreach (var message in _logEvents)
            {
                _target.Write(message);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _target?.Dispose();

                _disposed = true;
            }
        }

        private void PopulateLogEvents(Random random)
        {
            Enumerable.Range(0, 1000).ForEach(i =>
            {
                _logEvents.Add(
                    new LogEventInfo(
                        LogLevel.Info,
                        "loggerName",
                        string.Join(
                            string.Empty,
                            Enumerable.Range(0, random.Next(1000, 10000))
                                .Select(n => n.ToString("X")))));
            });
        }
    }
}
