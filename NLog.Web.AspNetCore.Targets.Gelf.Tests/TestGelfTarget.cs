namespace NLog.Web.AspNetCore.Targets.Gelf.Tests
{
    internal class TestGelfTarget : GelfTarget
    {
        public TestGelfTarget(ITransportFactory transportFactory, IConverter converter)
            : base(transportFactory, converter)
        {
        }

        public new void Write(LogEventInfo logEvent)
        {
            base.Write(logEvent);
        }

        public new void InitializeTarget()
        {
            base.InitializeTarget();
        }
    }
}