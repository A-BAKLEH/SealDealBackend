using Serilog.Core;
using Serilog.Events;

namespace Web.Config;
public class RemovePropertiesEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent le, ILogEventPropertyFactory lepf)
    {
        le.RemovePropertyIfPresent("Type");
        le.RemovePropertyIfPresent("ObjectResultType");
        le.RemovePropertyIfPresent("ActionId");
        le.RemovePropertyIfPresent("addresses");
        le.RemovePropertyIfPresent("EventId");
        le.RemovePropertyIfPresent("elapsed");
        le.RemovePropertyIfPresent("newLine");
        le.RemovePropertyIfPresent("Protocol");
        le.RemovePropertyIfPresent("parameters");
        le.RemovePropertyIfPresent("Method");
        le.RemovePropertyIfPresent("Scheme");
        le.RemovePropertyIfPresent("RequestId");
        le.RemovePropertyIfPresent("Host");
        le.RemovePropertyIfPresent("ConnectionId");
        le.RemovePropertyIfPresent("connectionId");
        le.RemovePropertyIfPresent("ContentLength");
        le.RemovePropertyIfPresent("commandType");
        le.RemovePropertyIfPresent("commandTimeout");
    }
}