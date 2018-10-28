namespace NLog.Web.AspNetCore.Targets.Gelf
{
    internal enum SyslogSeverities
    {
        Emergency = 0,
        Alert, 
        Critical, 
        Error,
        Warning,
        Notice,
        Informational,
        Debug,
    }
}