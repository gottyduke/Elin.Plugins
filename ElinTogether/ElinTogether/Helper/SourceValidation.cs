using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EModding.Helper.Runtime;

namespace ElinTogether.Helper;

public class SourceValidation
{
    // ReSharper disable StringLiteralTypo
    public static readonly ImmutableArray<string> ExcludedPlugins = [
        "com.sinai.unityexplorer",
        "jp.cmbc.mod.elin.yk-devtool",
    ];

    public static readonly ImmutableArray<string> DefaultSources = [
        nameof(SourceBlock),
        nameof(SourceChara),
        nameof(SourceElement),
        nameof(SourceFaction),
        nameof(SourceHobby),
        nameof(SourceThing),
        nameof(SourceJob),
        nameof(SourceMaterial),
        nameof(SourceObj),
        nameof(SourceQuest),
        nameof(SourceRace),
        nameof(SourceRecipe),
        nameof(SourceReligion),
        nameof(SourceStat),
        nameof(SourceZone),
    ];

    public static readonly Dictionary<int, Type> IdToActMapping = [];
    public static readonly Dictionary<Type, int> ActToIdMapping = [];

    public static Dictionary<string, string> GenerateAll(IEnumerable<string> sourceTypes)
    {
        return sourceTypes.Distinct().ToDictionary(s => s, GenerateSourceChecksum);
    }

    public static string GenerateSourceChecksum(string sourceType)
    {
        return ModUtil.FindSourceByName(sourceType).ExportRows().ToCompactJson().GetSha256Code();
    }

    public static void BuildActMapping()
    {
        ActToIdMapping.Clear();
        IdToActMapping.Clear();

        var actType = typeof(Act);
        var allActs = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => {
                try {
                    return a.GetTypes();
                } catch {
                    return [];
                }
            })
            .Where(actType.IsAssignableFrom)
            .OrderBy(GetInheritanceDepth)
            .ThenBy(t => t.Name);

        // keep NoGoal as 0 for bit checking
        IdToActMapping[0] = typeof(NoGoal);
        ActToIdMapping[typeof(NoGoal)] = 0;

        var actIndex = 1;
        foreach (var act in allActs) {
            if (!ActToIdMapping.TryAdd(act, actIndex)) {
                continue;
            }

            IdToActMapping[actIndex] = act;
            actIndex++;
        }

        return;

        static int GetInheritanceDepth(Type t)
        {
            var depth = 0;
            while (t.BaseType != null) {
                depth++;
                t = t.BaseType;
            }

            return depth;
        }
    }

    // TODO: verify this, but we will probably just leave a mismatch warning
    public static List<string> GetSourceAssemblies()
    {
        return TypeQualifier.Plugins
            .Select(p => p.GetType().Assembly)
            .Select(a => a.FullName)
            .ToList();
    }
}