using Newtonsoft.Json.Linq;

namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal interface IConverter
    {
        JObject GetGelfObject(LogEventInfo logEventInfo);

        IGelfTarget Target { get; set; }
    }
}