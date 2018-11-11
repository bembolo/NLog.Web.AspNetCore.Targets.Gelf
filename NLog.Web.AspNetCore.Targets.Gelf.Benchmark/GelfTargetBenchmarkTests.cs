using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CSharpx;
using NSubstitute;
using Xunit;

namespace NLog.Web.AspNetCore.Targets.Gelf.Benchmark
{
    public sealed class GelfTargetBenchmarkTests
    {
        [Fact]
        public void BenchmarkWrite()
        {
            _ = BenchmarkRunner.Run<GelfTargetBenchmark>();
        }

        [Fact]
        public void TestBenchmark()
        {
            var benchmark = new GelfTargetBenchmark();

            benchmark.VersionTwoPointZero();
        }
    }
}
