using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using ElinTogether.Models;

namespace ElinTogether.Helper;

public class SourceValidation
{
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
        SourceListType.Stats,
    ];

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
        var sources = EMono.sources;
        return type switch {
            SourceListType.Assembly => TypeQualifier.Plugins.Select(p => p.Info.Metadata.GUID),
            SourceListType.Card => sources.cards.rows.Select(r => r.id),
            SourceListType.Element => sources.elements.rows.Select(r => r.alias),
            SourceListType.Job => sources.jobs.rows.Select(r => r.id),
            SourceListType.Race => sources.races.rows.Select(r => r.id),
            SourceListType.Material => sources.materials.rows.Select(r => r.alias),
            SourceListType.Religion => sources.religions.rows.Select(r => r.id),
            SourceListType.Quest => sources.quests.rows.Select(r => r.id),
            SourceListType.Stats => sources.stats.rows.Select(r => r.alias),
            SourceListType.Zone => sources.zones.rows.Select(r => r.id),
            _ => DebugThrow.Return(new ArgumentOutOfRangeException(nameof(type)), new List<string>()),
        };
    }

    public static void ThrowIfInvalid(SourceListType sourceType)
    {
        if (sourceType is <= SourceListType.Reserved or > SourceListType.All) {
            ThrowHelper.Throw(new ArgumentOutOfRangeException(nameof(sourceType)),
                "Invalid source validation {SourceListType} requested", sourceType);
        }
    }
}