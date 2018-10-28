using Newtonsoft.Json;
using Xunit;

namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    public class GelfMessageTests
    {
        [Fact]
        public void ShouldSerializeJsonProperties()
        {
            var message = new GelfMessage()
            {
                Level = 4,
                Facility = "facility",
                FullMessage = "fullMessage",
                File = "file",
                Host = "host",
                Line = "line",
                ShortMessage = "shortMessage",
                Timestamp = 3d,
                Version = "version"
            };

            // Act
            var json = JsonConvert.SerializeObject(message);

            Assert.Equal(@"{""facility"":""facility"",""file"":""file"",""full_message"":""fullMessage"",""host"":""host"",""level"":4,""line"":""line"",""short_message"":""shortMessage"",""timestamp"":3.0,""version"":""version""}", json);
        }
    }
}