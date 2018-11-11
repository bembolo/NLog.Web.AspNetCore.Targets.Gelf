using Newtonsoft.Json.Linq;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface IConverter
    {
        IGelfTarget Target { get; set; }

        JObject GetGelfObject(LogEventInfo logEventInfo);
    }
}