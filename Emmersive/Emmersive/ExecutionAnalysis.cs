using System;
using System.IO;
using System.Linq;
using Cwl.Helper.String;
using Emmersive.Helper;

namespace Emmersive;

internal class ExecutionAnalysis
{
    internal static void CleanupActivities()
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
        CleanupActivities();

        var activities = EmActivity.GetAllSummaries().ToArray();
        if (activities.Length == 0) {
            return;
        }

        var file = $"Activity\\{DateTime.Now:MM_dd_hh_mm_ss}.csv";

        using var sb = StringBuilderPool.Get();
        sb.AppendLine("Service,RequestTotal,RequestSuccess,RequestFailure,TokensTotal,PromptUsage,StructuredOutput,Duration");

        foreach (var summary in activities) {
            sb.Append($"{summary.ServiceName},");
            sb.Append($"{summary.RequestTotal},");
            sb.Append($"{summary.RequestSuccess},");
            sb.Append($"{summary.RequestFailure},");
            sb.Append($"{summary.TokensTotal},");
            sb.Append($"{summary.TokensInput},");
            sb.Append($"{summary.TokensOutput},");
            sb.AppendLine($"{summary.TotalLatencySec}");
        }

        ResourceFetch.SetCustomResource(file, sb.ToString());
    }
}