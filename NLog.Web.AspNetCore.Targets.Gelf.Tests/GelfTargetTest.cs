using System;
using System.Linq;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Net;
using NLog.Layouts;
using NSubstitute;

namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    public class GelfTargetTests
    {
        private ITransportFactory _transportFactory;
        private ITransport _transport;
        private IConverter _converter;

        public GelfTargetTests()
        {
            _transportFactory = Substitute.For<ITransportFactory>();
            _transport = Substitute.For<ITransport>();
            _converter = Substitute.For<IConverter>();
        }

        [Fact]
        public void ShouldDisposeDisposeTransportWhenAvailable()
        {
            var target = new GelfTarget(_transportFactory, _converter);
            
            _transportFactory.CreateTransport(target).Returns(_transport);

            target.InternalWrite(new LogEventInfo { Message = "Test Message" });

            // Act
            target.Dispose();

            _transport.Received(1).Dispose();
        }

        [Fact]
        public void ShouldDefaultConstructorCreateTargetWithDefaultPropertiesSet()
        {
            // Act
            var target = new GelfTarget();

            // Assert
            Assert.NotNull(target.Parameters);
            Assert.Empty(target.Parameters);
            Assert.Null(target.Endpoint);
            Assert.Equal(1500, target.MaxUdpChunkSize);
            Assert.Equal(10, target.MaxNestedExceptionsDepth);
            Assert.Null(target.Facility);
            Assert.False(target.SendLastFormatParameter);
        }

        [Fact]
        public void ShouldConstructorCreateTarget()
        {
            // Act
            var target = new GelfTarget(_transportFactory, _converter);

            // Assert
            Assert.NotNull(target.Parameters);
            Assert.Empty(target.Parameters);
            _transportFactory.DidNotReceiveWithAnyArgs().CreateTransport(null);
        }

        [Fact]
        public void ShouldFacilitySet()
        {
            const string expectedFacility = "facility";
            // Act
            var target = new GelfTarget(_transportFactory, _converter)
            {
                Facility = expectedFacility
            };

            // Assert
            Assert.Equal(expectedFacility, target.Facility);
        }

        [Fact]
        public void ShouldEndpointNotSetIfUriIsInvalid()
        {
            // Act
            var target = new GelfTarget(_transportFactory, _converter)
            {
                Endpoint = "http://invalid Endpoint"
            };

            // Assert
            Assert.Null(target.Endpoint);
        }

        [Fact]
        public void ShouldEndpointSetIfValidUri()
        {
            const string expectedEndpoint = "udp://graylog.host.com:12201";

            // Act
            var target = new GelfTarget(_transportFactory, _converter)
            {
                Endpoint = expectedEndpoint
            };

            // Assert
            Assert.Equal(expectedEndpoint, target.Endpoint);
        }

        [Fact]
        public void ShouldWriteSendUdpMessage()
        {
            var target = new GelfTarget(_transportFactory, _converter);

            _transportFactory.CreateTransport(target).Returns(_transport);

            var logEventInfo = new LogEventInfo { Message = "Test Message" };

            var gelfObject = new JObject();
            _converter.GetGelfObject(logEventInfo).Returns(gelfObject);

            // Act
            target.InternalWrite(logEventInfo);

            // Assert
            _transportFactory.Received(1).CreateTransport(target);
            _transport.Received(1).Send(gelfObject);
            _converter.Received(1).GetGelfObject(logEventInfo);
        }

        [Fact]
        public void ShouldWriteNotSendMessageWhenTransportNotFound()
        {
            var target = new GelfTarget(_transportFactory, _converter);

            _transportFactory.CreateTransport(target).Returns(Substitute.For<ITransport>());

            var logEventInfo = new LogEventInfo { Message = "Test Message" };

            var gelfObject = new JObject();
            _converter.GetGelfObject(logEventInfo).ReturnsForAnyArgs(gelfObject);

            // Act
            target.InternalWrite(logEventInfo);

            // Assert
            _transportFactory.Received(1).CreateTransport(target);
            _transport.DidNotReceiveWithAnyArgs().Send(null);
            _converter.Received(1).GetGelfObject(logEventInfo);
        }

        [Fact]
        public void ShouldMaxUdpChunkSizeSetToDefaultIfNotProvided()
        {
            var target = new GelfTarget(_transportFactory, _converter);

            Assert.Equal(1500, target.MaxUdpChunkSize);
        }

        [Theory]
        [InlineData(-1, 576)]
        [InlineData(10000, 8192)]
        public void ShouldMaxUdpChunkSizeSetToCoercedValue(int value, int expectedValue)
        {
            var target = new GelfTarget(_transportFactory, _converter)
            {
                MaxUdpChunkSize = value
            };

            Assert.Equal(expectedValue, target.MaxUdpChunkSize);
        }

        [Fact]
        public void ShouldMaxNestedExceptionsDepthSetToDefaultIfNotProvided()
        {
            var target = new GelfTarget(_transportFactory, _converter);

            Assert.Equal(10, target.MaxNestedExceptionsDepth);
        }

        [Fact]
        public void ShouldMaxNestedExceptionsDepthSetToCoercedValue()
        {
            var target = new GelfTarget(_transportFactory, _converter)
            {
                MaxNestedExceptionsDepth = -1
            };

            Assert.Equal(0, target.MaxNestedExceptionsDepth);
        }

        [Fact]
        public void ShouldSendLastFormatParameterSetValue()
        {
            var target = new GelfTarget(_transportFactory, _converter)
            {
                SendLastFormatParameter = true
            };

            Assert.True(target.SendLastFormatParameter);
        }

        [Fact]
        public void ShouldEndpointUriReturnEndpointAsUri()
        {
            const string expectedEndpoint = "http://graylog.host:12201";
            IGelfTarget target = new GelfTarget(_transportFactory, _converter)
            {
                Endpoint = expectedEndpoint
            };

            Assert.Equal(expectedEndpoint, target.EndpointUri.OriginalString);
        }

        [Fact]
        public void ShouldWriteAddParametersToLogEvent()
        {
            var parameter = new GelfParameterInfo("parameterName", new SimpleLayout("${message}"));
            var target = new GelfTarget(_transportFactory, _converter)
            {
                Parameters = { parameter }
            };

            _transportFactory.CreateTransport(target).Returns(Substitute.For<ITransport>());

            var logEventInfo = new LogEventInfo { Message = "Test Message" };

            var gelfObject = new JObject();
            _converter.GetGelfObject(logEventInfo).ReturnsForAnyArgs(gelfObject);

            // Act
            target.InternalWrite(logEventInfo);

            // Assert
            Assert.NotEmpty(logEventInfo.Properties);
            Assert.Equal(1, logEventInfo.Properties.Count);
            var property = logEventInfo.Properties.First();
            Assert.Equal(parameter.Name, (string)property.Key);
            Assert.Equal(logEventInfo.Message, property.Value);
        }

        [Fact]
        public void ShouldWriteAddLastLogEventParametersAsObjectToLogEventWhenSendLastParameterIsTrue()
        {
            var target = new GelfTarget(_transportFactory, _converter)
            {
                SendLastFormatParameter = true
            };

            _transportFactory.CreateTransport(target).Returns(Substitute.For<ITransport>());

            var parameter = new {Hello = "World!"};
            var logEventInfo = new LogEventInfo
            {
                Parameters = new[]{ parameter }
            };

            var gelfObject = new JObject();
            _converter.GetGelfObject(logEventInfo).ReturnsForAnyArgs(gelfObject);

            // Act
            target.InternalWrite(logEventInfo);

            // Assert
            Assert.NotEmpty(logEventInfo.Properties);
            Assert.Equal(1, logEventInfo.Properties.Count);
            var property = logEventInfo.Properties.First();
            Assert.Equal(ConverterConstants.PromoteObjectPropertiesMarker, property.Key);
            Assert.Equal(parameter, property.Value);
        }
    }
}