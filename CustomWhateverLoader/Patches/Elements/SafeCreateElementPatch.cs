using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Elements;

[HarmonyPatch]
internal class SafeCreateElementPatch
{
    private const int LogSpamMax = 4;
    private static readonly Dictionary<int, int> _prompted = [];

    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Element), nameof(Element.Create), typeof(int), typeof(int))]
    internal static IEnumerable<CodeInstruction> OnCreateIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new(OpCodes.Ldstr, nameof(Element)),
                new(OpCodes.Call),
                new(OpCodes.Ldstr, "Elin"),
                new OperandContains(OpCodes.Call, nameof(ClassCache.Create)))
            .EnsureValid("create element")
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(SafeCreateInvoke))
            .RemoveInstruction()
            .InstructionEnumeration();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Act), nameof(Act.Perform), [])]
    internal static bool OnPerformSafetyCone(Act __instance)
    {
        var id = __instance.source.id;
        if (!Act.CC.elements.dict.TryGetValue(id, out var element) ||
            element is not CustomElement custom) {
            return true;
        }

        Act.CC.elements.Remove(id);
        if (Act.CC.IsPC) {
            custom.RemoveSafetyCone();
        }

        return false;
    }

    [Time]
    private static Element SafeCreateInvoke(string unqualified, string assembly, int id)
    {
        try {
            var element = ClassCache.Create<Element>(unqualified, assembly);
            if (element is not null) {
                return element;
            }

            throw new SourceParseException("cwl_warn_deserialize");
        } catch {
            _prompted.TryAdd(id, 0);
            _prompted[id]++;

            if (_prompted[id] <= LogSpamMax) {
                CwlMod.WarnWithPopup<CustomElement>(_prompted[id] == LogSpamMax
                    ? "cwl_warn_deserialize_final".Loc()
                    : "cwl_warn_deserialize".Loc(nameof(Element), id, unqualified,
                        CwlConfig.Patches.SafeCreateClass!.Definition.Key));
            }
            // noexcept
        }

        var row = EMono.sources.elements.map.TryGetValue(id)!;
        row.name = "cwl_type_safety_cone".Loc(nameof(Element), id, row.alias, unqualified);
        row.detail = "cwl_type_safety_desc".Loc();

        return new CustomElement();
    }
}