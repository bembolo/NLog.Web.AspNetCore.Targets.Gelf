using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CSharpx;
using NSubstitute;
using Xunit;

namespace NLog.Web.AspNetCore.Targets.Gelf.Benchmark
{
    [CoreJob]
    public class GelfTargetBenchmark
    {
        private IList<LogEventInfo> _logEvents;
        private GelfConverter _converter;
        private IUdpClient _udpClient;
        private UdpTransport _udpTransport;
        private ITransportFactory _transportFactory;
        private GelfTarget _target;

        public GelfTargetBenchmark()
        {
            var random = new Random(1);
            _logEvents = new List<LogEventInfo>();

            PopulateLogEvents(random);

            _converter = new GelfConverter(Substitute.For<IDns>());
            _udpClient = new StubUdpClient();
            _udpTransport = new UdpTransport(_udpClient, 1500);
            _transportFactory = Substitute.For<ITransportFactory>();

            _target = new GelfTarget(_transportFactory, _converter)
            {
                SendLastFormatParameter = true,
            };

            _transportFactory.CreateTransport(_target).Returns(_udpTransport);
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

        [Fact]
        public void BenchmarkWrite()
        {
            var summary = BenchmarkRunner.Run<GelfTargetBenchmark>();
        }

        [Fact]
        public void TestBenchmark()
        {
            VersionTwoPointZero();
        }

        [Benchmark]
        public void VersionTwoPointZero()
        {
            foreach (var message in _logEvents)
            {
                _target.InternalWrite(message);                
            }
        }
    }
}
