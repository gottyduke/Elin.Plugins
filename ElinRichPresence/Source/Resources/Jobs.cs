using System.Collections.Generic;
using System.Linq;

namespace Erpc.Resources;

internal static class Jobs
{
    internal static List<string> BuiltInJobs => [
        "archer",
        "executioner",
        "farmer",
        "gunner",
        "inquisitor",
        "paladin",
        "pianist",
        "priest",
        "swordsage",
        "thief",
        "tourist",
        "warmage",
        "warrior",
        "witch",
        "wizard",
    ];

    internal static string GetJobText(this SourceJob.Row job)
    {
        var name = string.Join(' ', job.name.Split(' ')
            .Select(n => char.ToUpper(n[0]) + n[1..]));
        return LocHelper.GetLangCode() switch {
            "JP" => job.name_JP,
            "EN" => name,
            _ => string.IsNullOrWhiteSpace(job.name_L) ? name : job.name_L,
        };
    }
}