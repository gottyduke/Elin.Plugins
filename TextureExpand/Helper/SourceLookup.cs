using System;
using System.Collections.Generic;
using System.Linq;

namespace TextureExpand.Helper;

internal static class SourceLookup
{
    private static readonly Dictionary<string, SourceElement.Row?> _elements = [];
    private static readonly Dictionary<string, SourceStat.Row?> _stats = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SourceMaterial.Row?> _materials = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SourceFloor.Row?> _floors = new(StringComparer.OrdinalIgnoreCase);

    internal static SourceElement.Row? FindElement(string alias)
    {
        if (!_elements.TryGetValue(alias, out var row)) {
            _elements[alias] = row = EClass.sources.elements.rows.FirstOrDefault(r => r.alias == alias);
        }

        return row;
    }

    internal static SourceStat.Row? FindStat(string alias)
    {
        if (!_stats.TryGetValue(alias, out var row)) {
            _stats[alias] = row =
                EClass.sources.stats.rows.FirstOrDefault(r => r.alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        }

        return row;
    }

    internal static SourceMaterial.Row? FindMaterial(string alias)
    {
        if (!_materials.TryGetValue(alias, out var row)) {
            _materials[alias] =
                row = EClass.sources.materials.rows.FirstOrDefault(r =>
                    r.alias.Contains(alias, StringComparison.OrdinalIgnoreCase));
        }

        return row;
    }

    internal static SourceFloor.Row? FindFloor(string alias)
    {
        if (!_floors.TryGetValue(alias, out var row)) {
            _floors[alias] = row =
                EClass.sources.floors.rows.FirstOrDefault(r => r.alias.Contains(alias, StringComparison.OrdinalIgnoreCase));
        }

        return row;
    }

    internal static bool HasReligion(string id)
    {
        return EClass.sources.religions.rows.Any(r => r.id == id);
    }
}