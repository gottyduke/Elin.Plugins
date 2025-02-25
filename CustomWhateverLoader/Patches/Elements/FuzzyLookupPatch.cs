using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class FuzzyLookup
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Core), nameof(Core.GetElement))]
    internal static IEnumerable<CodeInstruction> OnGetElementIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Ldloca_S),
                new OperandContains(OpCodes.Callvirt, nameof(EMono.sources.elements.alias.TryGetValue)))
            .SetInstruction(
                Transpilers.EmitDelegate(TryFuzzyGetValue))
            .InstructionEnumeration();
    }

    private static bool TryFuzzyGetValue(Dictionary<string, SourceElement.Row> alias, string id, out SourceElement.Row row)
    {
        id = id.IsEmpty("_void");
        if (alias.TryGetValue(id, out row)) {
            return true;
        }

        // O(n) + strlen^2
        foreach (var (name, ele) in alias) {
            if (!string.Equals(name, id, StringComparison.InvariantCultureIgnoreCase)) {
                continue;
            }

            row = ele;
            CwlMod.Log<FuzzyLookup>($"{id} => {name}");
            return true;
        }

        CwlMod.Warn<FuzzyLookup>($"cannot find element: {id}");
        return false;
    }
}