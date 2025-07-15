using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.Helper.String;
using Cwl.LangMod;

namespace CustomizerMinus.Helper;

public static class PartExt
{
    private static readonly Dictionary<PCC.Part, BaseModPackage> _cached = [];

    public static string GetPartProviderString(this PCC.Part part)
    {
        return part.id +
               $"\n<i>{part.GetPartProvider().title.TagColor(0x4ffff9)}</i>\n" +
               $"{part.dir.ShortPath()}";
    }

    public static BaseModPackage GetPartProvider(this PCC.Part part)
    {
        if (_cached.TryGetValue(part, out var provider)) {
            return provider;
        }

        var dir = part.dir;
        var mod = BaseModManager.Instance.packages
            .FirstOrDefault(p => dir.StartsWith(p.dirInfo.FullName));
        provider = mod ?? new() {
            title = "cmm_ui_unknown".Loc(),
        };

        return _cached[part] = provider;
    }

    public static int GetPartSortOrder(this PCC.Part part)
    {
        var order = BaseModManager.Instance.packages.IndexOf(part.GetPartProvider());
        if (order == -1) {
            order = int.MaxValue;
        }

        return order;
    }

    public static int PartSorter(PCC.Part lhs, PCC.Part rhs)
    {
        var lhsSort = lhs.GetPartSortOrder();
        var rhsSort = rhs.GetPartSortOrder();
        if (lhsSort != rhsSort) {
            return lhsSort - rhsSort;
        }

        var lhsIdIntegral = int.TryParse(lhs.id, out var lhsId);
        var rhsIdIntegral = int.TryParse(rhs.id, out var rhsId);

        if (lhsIdIntegral && rhsIdIntegral) {
            return lhsId.CompareTo(rhsId);
        }

        if (lhsIdIntegral) {
            return -1;
        }

        if (rhsIdIntegral) {
            return 1;
        }

        var idRgx = new Regex(@"(\d+)|(\D+)");
        var lhsMatches = idRgx.Matches(lhs.id);
        var rhsMatches = idRgx.Matches(rhs.id);

        var i = 0;
        var j = 0;
        while (i < lhsMatches.Count && j < rhsMatches.Count) {
            var lhsPart = lhsMatches[i].Value;
            var rhsPart = rhsMatches[j].Value;

            var isNumA = int.TryParse(lhsPart, out var numA);
            var isNumB = int.TryParse(rhsPart, out var numB);

            if (isNumA && isNumB) {
                var numComparison = numA.CompareTo(numB);
                if (numComparison != 0) {
                    return numComparison;
                }
            } else {
                var cmp = string.Compare(lhsPart, rhsPart,
                    StringComparison.InvariantCultureIgnoreCase);
                if (cmp != 0) {
                    return cmp;
                }
            }

            i++;
            j++;
        }

        return lhsMatches.Count.CompareTo(rhsMatches.Count);
    }
}