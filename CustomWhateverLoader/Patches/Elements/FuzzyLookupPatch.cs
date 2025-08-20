using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class FuzzyLookup
{
    private static Dictionary<string, SourceElement.Row> _lookup = [];
    private static int _hash = -1;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Core), nameof(Core.GetElement))]
    internal static IEnumerable<CodeInstruction> OnGetElementIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Ldloca_S),
                new OperandContains(OpCodes.Callvirt, nameof(EMono.sources.elements.alias.TryGetValue)))
            .EnsureValid("get element alias")
            .SetInstruction(
                Transpilers.EmitDelegate(TryFuzzyGetValue))
            .InstructionEnumeration();
    }

    private static bool TryFuzzyGetValue(Dictionary<string, SourceElement.Row> aliasMap, string alias, out SourceElement.Row row)
    {
        alias = alias.IsEmpty("_void");

        var newHash = aliasMap.GetContentHashCode();
        if (_hash != newHash) {
            _lookup = new(aliasMap, StringComparer.OrdinalIgnoreCase);
            _hash = newHash;
        }

        if (aliasMap.TryGetValue(alias, out row)) {
            return true;
        }

        if (_lookup.TryGetValue(alias, out row)) {
            CwlMod.Log<FuzzyLookup>($"{alias} => {row.alias}");
            return true;
        }

        CwlMod.Warn<FuzzyLookup>("cwl_warn_fuzzy_lookup".Loc(alias));
        return false;
    }
}