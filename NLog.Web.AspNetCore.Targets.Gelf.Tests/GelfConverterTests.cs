using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    public class GelfConverterTests
    {
        [Fact]
        public void ShouldGetGelfJsonAddMappedDiagnosticsLogicalContextData()
        {
            MappedDiagnosticsLogicalContext.Set("test", "value");

            var logEvent = LogEventInfo.Create(LogLevel.Info, "loggerName", null, "message");

            var converter = new GelfConverter();

            // Act
            var gelfJson = converter.GetGelfJson(logEvent, "facility");

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

            var converter = new GelfConverter();

            // Act
            var gelfJson = converter.GetGelfJson(logEvent, "facility");

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

            var converter = new GelfConverter();

            // Act
            var gelfJson = converter.GetGelfJson(logEvent, "facility");

            Assert.Equal("anotherValue", gelfJson.Value<string>("_test"));
        }
    }
}