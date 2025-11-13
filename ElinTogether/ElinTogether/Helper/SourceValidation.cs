using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using ElinTogether.Models;
using HarmonyLib;

namespace ElinTogether.Helper;

public class SourceValidation
{
    private static readonly string[] _excludedPlugins = [
        "com.sinai.unityexplorer",
    ];

    private static readonly SourceListType[] _validationRequirements = [
        SourceListType.Assembly,
        SourceListType.Card,
        SourceListType.Zone,
        SourceListType.Element,
        SourceListType.Job,
        SourceListType.Race,
        SourceListType.Material,
        SourceListType.Religion,
        SourceListType.Quest,
        SourceListType.Stat,
        SourceListType.Act,
    ];

    // 255 should be more than enough
    public static readonly Dictionary<ushort, Type> IdToActMapping = [];
    public static readonly Dictionary<Type, ushort> ActToIdMapping = [];

    public static Dictionary<SourceListType, byte[]> GenerateAll()
    {
        var data = new Dictionary<SourceListType, byte[]>();

        foreach (var sourceType in _validationRequirements) {
            data[sourceType] = GenerateSourceChecksum(sourceType);
        }

        return data;
    }

    public static byte[] GenerateSourceChecksum(SourceListType type)
    {
        using var sb = StringBuilderPool.Get();
        foreach (var id in GenerateSourceIdList(type).OrderBy(id => id, StringComparer.Ordinal)) {
            sb.Append(id).Append('|');
        }

        return sb.ToString().GetSha256Hash().ToArray();
    }

    public static IEnumerable<string> GenerateSourceIdList(SourceListType type)
    {
        if (ActToIdMapping.Count == 0) {
            BuildAiActMapping();
        }

        var sources = EMono.sources;
        return type switch {
            SourceListType.Assembly => TypeQualifier.Plugins
                .Select(p => p.Info.Metadata.GUID)
                .Except(_excludedPlugins),
            SourceListType.Card => sources.cards.rows.Select(r => r.id),
            SourceListType.Element => sources.elements.rows.Select(r => r.alias),
            SourceListType.Job => sources.jobs.rows.Select(r => r.id),
            SourceListType.Race => sources.races.rows.Select(r => r.id),
            SourceListType.Material => sources.materials.rows.Select(r => r.alias),
            SourceListType.Religion => sources.religions.rows.Select(r => r.id),
            SourceListType.Quest => sources.quests.rows.Select(r => r.id),
            SourceListType.Stat => sources.stats.rows.Select(r => r.alias),
            SourceListType.Zone => sources.zones.rows.Select(r => r.id),
            SourceListType.Act => ActToIdMapping.Keys.Select(t => t.Name),
            _ => DebugThrow.Return(new ArgumentOutOfRangeException(nameof(type)), Array.Empty<string>()),
        };
    }

    public static void ThrowIfInvalid(SourceListType sourceType)
    {
        if (sourceType is <= SourceListType.Reserved or > SourceListType.All) {
            ThrowHelper.Throw(new ArgumentOutOfRangeException(nameof(sourceType)),
                "Invalid source validation {SourceListType} requested", sourceType);
        }
    }

    public static void BuildAiActMapping()
    {
        ActToIdMapping.Clear();
        IdToActMapping.Clear();

        var actType = typeof(Act);
        var allActs = AccessTools.AllAssemblies()
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

        byte actIndex = 1;
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
}