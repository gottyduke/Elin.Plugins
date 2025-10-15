using System;
using Cwl.Helper.String;
using Emmersive.Helper;

namespace Emmersive;

internal class ExecutionAnalysis
{
    internal static void DumpSessionActivities()
    {
        var file = $"Activity\\{DateTime.Now:MM_dd_hh_mm_ss}.csv";

        using var sb = StringBuilderPool.Get();
        sb.AppendLine("Service,RequestTotal,RequestSuccess,RequestFailure,TokensTotal,PromptUsage,StructuredOutput,Duration");

        foreach (var summary in EmActivity.GetAllSummaries()) {
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