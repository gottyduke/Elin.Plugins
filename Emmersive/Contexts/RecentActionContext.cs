using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;

namespace Emmersive.Contexts;

public class RecentActionContext : ContextProviderBase
{
    internal static readonly List<string> Session = [];
    public override string Name => "previous_action_log";

    public override object? Build()
    {
        var logs = Session
            .TakeLast(EmConfig.Context.RecentLogDepth.Value)
            .ToArray();

        return logs.Length == 0
            ? null
            : logs;
    }

    public static void Add(string entry)
    {
        if (Session.Count > 0) {
            if (Session[^1] == entry) {
                return;
            }
        }

        Session.Add(entry);
    }

    [CwlPostLoad]
    public static void ClearSession(GameIOProcessor.GameIOContext context)
    {
        Session.Clear();
    }
}