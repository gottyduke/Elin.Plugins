using System;
using System.IO;
using System.Linq;
using Cwl.Helper.String;
using Emmersive.Helper;

namespace Emmersive;

internal class ExecutionAnalysis
{
    internal static void CleanupActivityLogs()
    {
        var logs = Path.Combine(ResourceFetch.CustomFolder, "Activity");
        if (!Directory.Exists(logs)) {
            return;
        }

        var files = new DirectoryInfo(logs)
            .GetFiles()
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Skip(10);

        foreach (var file in files) {
            try {
                file.Delete();
            } catch {
                // noexcept
            }
        }
    }

    internal static void DumpSessionActivities()
    {
        using var _ = EmPromptReset.ScopedNotifyChanges(false);

        CleanupActivityLogs();

        var activities = EmActivity.Session;
        if (activities.Count == 0) {
            return;
        }

        var file = $"Activity\\{DateTime.UtcNow:MM_dd_hh_mm_ss}.csv";

        using var sb = StringBuilderPool.Get();
        sb.AppendLine("Activity,Service,Status,Latency,Input,Output");

        foreach (var activity in activities) {
            sb.Append($"{activity.ActivityId},");
            sb.Append($"{activity.ServiceName},");
            sb.Append($"{activity.Status},");
            sb.Append($"{activity.Latency.TotalMilliseconds},");
            sb.Append($"{activity.TokensInput},");
            sb.AppendLine($"{activity.TokensOutput}");
        }

        ResourceFetch.SetCustomResource(file, sb.ToString());
    }
}