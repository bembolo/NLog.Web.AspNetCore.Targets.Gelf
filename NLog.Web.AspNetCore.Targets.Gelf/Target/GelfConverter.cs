using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NLog.Common;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal class GelfConverter : IConverter
    {
        internal const int DefaultMaxNestedExceptionsDepth = 10;

        private const int ShortMessageMaxLength = 250;
        private const string GelfVersion = "1.1";

        // http://docs.graylog.org/en/2.4/pages/gelf.html#gelf-payload-specification
        private static readonly int[] _severityMapping = new[]
        {
            /* LogLevel.Trace.Ordinal => */ (int)SyslogSeverities.Debug,
            /* LogLevel.Debug.Ordinal => */ (int)SyslogSeverities.Debug,
            /* LogLevel.Info.Ordinal =>  */ (int)SyslogSeverities.Informational,
            /* LogLevel.Warn.Ordinal =>  */ (int)SyslogSeverities.Warning,
            /* LogLevel.Error.Orindal => */ (int)SyslogSeverities.Error,
            /* LogLevel.Fatal.Ordinal => */ (int)SyslogSeverities.Critical,
        };

        private readonly Lazy<string> _facility;
        private readonly Lazy<int> _maxNestedExceptionsDepth;
        private readonly string _hostName;

        public GelfConverter(IDns dns)
        {
            _facility = new Lazy<string>(() => string.IsNullOrEmpty(Target?.Facility)
                ? "GELF" // Spec says: facility must be set by the client to "GELF" if empty
                : Target.Facility);
            _maxNestedExceptionsDepth =
                new Lazy<int>(() => Target?.MaxNestedExceptionsDepth ?? DefaultMaxNestedExceptionsDepth);
            _hostName = dns.GetHostName();
        }

        public IGelfTarget Target { get; set; }

        public JObject GetGelfObject(LogEventInfo logEventInfo)
        {
            // Retrieve the formatted message from LogEventInfo
            if (logEventInfo == null || logEventInfo.FormattedMessage == null || logEventInfo.Level == LogLevel.Off)
                return null;

            var logEventMessage = logEventInfo.FormattedMessage;

            // If we are dealing with an exception, pass exception properties to LogEventInfo properties
            if (logEventInfo.Exception != null)
            {
                (var exceptionDetail, var stackDetail) = GetExceptionDetails(logEventInfo.Exception, _maxNestedExceptionsDepth.Value);

                logEventInfo.Properties.Add("ExceptionSource", logEventInfo.Exception.Source);
                logEventInfo.Properties.Add("ExceptionMessage", exceptionDetail);
                logEventInfo.Properties.Add("StackTrace", stackDetail);
            }

            // Figure out the short message
            var shortMessage = logEventMessage;
            if (shortMessage.Length > ShortMessageMaxLength)
            {
                shortMessage = shortMessage.Substring(0, ShortMessageMaxLength);
            }

            // Construct the instance of GelfMessage
            // See http://docs.graylog.org/en/2.4/pages/gelf.html#gelf-payload-specification "Specification (version 1.1)"
            var gelfMessage = new GelfMessage
            {
                Version = GelfVersion,
                Host = _hostName,
                ShortMessage = shortMessage,
                FullMessage = logEventMessage,
                Timestamp = new DateTimeOffset(logEventInfo.TimeStamp).ToUnixTimeMilliseconds() / 1000d,
                Level = _severityMapping[logEventInfo.Level.Ordinal],
                Facility = _facility.Value,
                Line = (logEventInfo.UserStackFrame != null)
                                                 ? logEventInfo.UserStackFrame.GetFileLineNumber().ToString(
                                                     CultureInfo.InvariantCulture)
                                                 : string.Empty,
                File = (logEventInfo.UserStackFrame != null)
                                                 ? logEventInfo.UserStackFrame.GetFileName()
                                                 : string.Empty,
            };

            // Add any other interesting data to LogEventInfo properties
            logEventInfo.Properties.Add("LoggerName", logEventInfo.LoggerName);

            // adding MappedDiagnosticsLogicalContext data
            MappedDiagnosticsLogicalContext.GetNames()
                .Select(n => (Name: n, Value: MappedDiagnosticsLogicalContext.GetObject(n)))
                .ToList()
                .ForEach(t =>
                {
                    if (!logEventInfo.Properties.ContainsKey(t.Name))
                        logEventInfo.Properties.Add(t.Name, t.Value);
                });

            var jObject = JObject.FromObject(gelfMessage);

            // We will persist them "Additional Fields" according to Gelf spec
            foreach (var property in logEventInfo.Properties)
            {
                AddAdditionalField(jObject, property);
            }

            return jObject;
        }

        internal static (string exceptionDetail, string stackDetail) GetExceptionDetails(Exception ex, int maxNestedExceptionsDepth)
        {
            const string ExceptionMessageSeparator = " ---> ";
            const string StackTraceSeparator = "--- Inner exception stack trace ---";

            var exceptionDetail = new StringBuilder();
            var stackDetail = new StringBuilder();

            void InsertExceptionMessage(Exception exception, StringBuilder stringBuilder, int level)
            {
                stringBuilder.Append(exception.GetType().FullName);
                stringBuilder.Append(": ");
                stringBuilder.Append(exception.Message);

                if (level < maxNestedExceptionsDepth && exception.InnerException != null)
                {
                    stringBuilder.Append(ExceptionMessageSeparator);

                    InsertExceptionMessage(exception.InnerException, stringBuilder, level + 1);
                }
            }

            void InsertStackDetail(Exception exception, StringBuilder stringBuilder, int level)
            {
                if (level < maxNestedExceptionsDepth && exception.InnerException != null)
                    InsertStackDetail(exception.InnerException, stringBuilder, level + 1);

                if (exception.StackTrace != null)
                {
                    stackDetail.AppendLine(exception.StackTrace);

                    if (level > 0)
                        stackDetail.AppendLine(StackTraceSeparator);
                }
            }

            InsertStackDetail(ex, stackDetail, 0);
            InsertExceptionMessage(ex, exceptionDetail, 0);

            return (exceptionDetail.ToString(), stackDetail.ToString());
        }

        private static void AddAdditionalField(JObject jObject, KeyValuePair<object, object> property)
        {
            if (property.Key == ConverterConstants.PromoteObjectPropertiesMarker &&
                property.Value != null &&
                property.Value is object obj)
            {
                try
                {
                    var jo = JObject.FromObject(obj);
                    foreach (var joProp in jo)
                    {
                        AddAdditionalField(jo, new KeyValuePair<object, object>(joProp.Key, joProp.Value));
                    }
                }
                catch (Exception ex)
                {
                    InternalLogger.Warn(ex, () => $"Unable to add additional field!");
                }
            }
            else if (property.Key is string propertyKey)
            {
                // According to the GELF spec, libraries should NOT allow to send id as additional field (_id)
                // Server MUST skip the field because it could override the MongoDB _key field
                if (propertyKey.Equals("id", StringComparison.OrdinalIgnoreCase))
                    propertyKey = "id_";

                // According to the GELF spec, additional field keys should start with '_' to avoid collision
                if (!propertyKey.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                    propertyKey = "_" + propertyKey;

                JToken value = null;
                if (property.Value != null)
                    value = JToken.FromObject(property.Value);

                jObject.Add(propertyKey, value);
            }
        }
    }
}