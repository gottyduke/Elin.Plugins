using ElinTogether.Net;
using Serilog.Core;
using Serilog.Events;

namespace ElinTogether;

internal class NetSessionStateEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!NetSession.Instance.HasActiveConnection) {
            return;
        }

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("NetSession", NetSession.Instance, true));
    }
}