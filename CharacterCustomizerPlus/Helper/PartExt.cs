using System.Collections.Generic;
using System.Linq;
using Cwl.LangMod;

namespace CustomizerMinus.Helper;

public static class PartExt
{
    private static readonly Dictionary<PCC.Part, string> _cached = [];

    public static string GetPartProvider(this PCC.Part part)
    {
        if (_cached.TryGetValue(part, out var provider)) {
            return provider;
        }

        var dir = part.dir;
        var mod = BaseModManager.Instance.packages
            .FirstOrDefault(p => dir.StartsWith(p.dirInfo.FullName));
        provider = mod is not null
            ? mod.title
            : "cmm_ui_unknown".Loc();

        return _cached[part] = provider;
    }
}