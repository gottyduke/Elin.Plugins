using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Custom;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Conditions;

[HarmonyPatch]
internal class SafeCreateConditionPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Condition), nameof(Condition.Create), typeof(string), typeof(int), typeof(Action<Condition>))]
    internal static IEnumerable<CodeInstruction> OnCreateIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Ldstr, "Elin"),
                new CodeMatch(o => o.opcode == OpCodes.Call &&
                                   o.operand.ToString().Contains(nameof(ClassCache.Create))))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(SafeCreateInvoke))
            .RemoveInstruction()
            .InstructionEnumeration();
    }

    [Time]
    private static Condition SafeCreateInvoke(string unqualified, string assembly, string alias)
    {
        try {
            var condition = ClassCache.Create<Condition>(unqualified, assembly);
            if (condition is null) {
                throw new SourceParseException("cwl_warn_create_ele");
            }

            return condition;
        } catch (Exception ex) {
            CwlMod.Warn(ex.Message.Loc(alias, unqualified, CwlConfig.Patches.SafeCreateClass!.Definition.Key));
            // noexcept
        }

        var row = EMono.sources.stats.alias.TryGetValue(alias)!;
        row.name = "cwl_ele_safety_cone".Loc(row.id, alias, unqualified);
        row.detail = "cwl_ele_safety_desc".Loc();

        return new CustomCondition();
    }
}