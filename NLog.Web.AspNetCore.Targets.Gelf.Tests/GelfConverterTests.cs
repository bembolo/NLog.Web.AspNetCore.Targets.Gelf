using Newtonsoft.Json;
using System;
using NSubstitute;
using Xunit;
using System.Text.RegularExpressions;

namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    public class GelfConverterTests
    {
        private IDns _dns;
        private GelfConverter _converter;

        public GelfConverterTests()
        {
            _dns = Substitute.For<IDns>();
            _converter = new GelfConverter(_dns);
        }

        [Fact]
        public void ShouldGetGelfJsonAddMappedDiagnosticsLogicalContextData()
        {
            MappedDiagnosticsLogicalContext.Set("test", "value");

            var logEvent = LogEventInfo.Create(LogLevel.Info, "loggerName", null, "message");

            // Act
            var gelfJson = _converter.GetGelfObject(logEvent);

            Assert.Equal("value", gelfJson.Value<string>("_test"));
        }

        // http://docs.graylog.org/en/2.4/pages/gelf.html#gelf-payload-specification
        [Theory]
        [InlineData(1540711622898, "1540711622.898")]
        [InlineData(1540711622890, "1540711622.89")]
        [InlineData(1540711622800, "1540711622.8")]
        [InlineData(1540711622000, "1540711622")]
        public void ShouldGetGelfJsonProvideJsonWithValidUnixEpochTimestampHavingMaxThreeDigitDecimalFractions(long unixEpochMilliseconds, string expectedSerialized)
        {
            var logEvent = LogEventInfo.Create(LogLevel.Info, "loggerName", null, "message");
            logEvent.TimeStamp = DateTimeOffset.FromUnixTimeMilliseconds(unixEpochMilliseconds).UtcDateTime;

            // Act
            var gelfJson = _converter.GetGelfObject(logEvent);

            // Assert
            var jToken = gelfJson["timestamp"];
            var value = gelfJson.Value<double>("timestamp");
            var serializedTimestamp = jToken.ToString(Formatting.None, null);

            Assert.Matches(expectedSerialized, serializedTimestamp);
        }

        [Fact]
        public void ShouldGetGelfJsonDiscardMappedDiagnosticsLogicalContextDataIfPresentInLogEventInfo()
        {
            MappedDiagnosticsLogicalContext.Set("test", "value");
            
            var logEvent = LogEventInfo.Create(LogLevel.Info, "loggerName", null, "message");
            logEvent.Properties.Add("test", "anotherValue");

            // Act
            var gelfJson = _converter.GetGelfObject(logEvent);

            Assert.Equal("anotherValue", gelfJson.Value<string>("_test"));
        }

        [Fact]
        public void ShouldGetGelfObject()
        {
            var logEvent = LogEventInfo.Create(LogLevel.Info, "loggerName", null, "message");
            logEvent.Properties.Add("test", "anotherValue");
            logEvent.Exception = new InvalidOperationException("test exception");

            // Act
            var gelfJson = _converter.GetGelfObject(logEvent);

            Assert.Equal("anotherValue", gelfJson.Value<string>("_test"));
        }

        [Fact]
        public void ShouldGetExceptionDetailsGetNestedExceptionDetails()
        {
            try
            {
                try
                {
                    throw new ArgumentException("argument exception");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("invalid operation", ex);
                }
            }
            catch (Exception exception)
            {
                // Act
                (string exceptionDetail, string stackDetail) = GelfConverter.GetExceptionDetails(exception, int.MaxValue);

                Assert.Equal("System.InvalidOperationException: invalid operation ---> System.ArgumentException: argument exception", exceptionDetail);

                var lines = stackDetail.Split(Environment.NewLine);
                Assert.StartsWith(@"   at NLog.Web.AspNetCore.Targets.Gelf.Tests.GelfConverterTests.ShouldGetExceptionDetailsGetNestedExceptionDetails() in ", lines[0]);
                Assert.EndsWith(@"line 89", lines[0]);
                Assert.Equal("--- Inner exception stack trace ---", lines[1]);
                Assert.StartsWith(@"   at NLog.Web.AspNetCore.Targets.Gelf.Tests.GelfConverterTests.ShouldGetExceptionDetailsGetNestedExceptionDetails() in ", lines[2]);
                Assert.EndsWith(@"line 93", lines[2]);
            }
        }
    }
}