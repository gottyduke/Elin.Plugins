using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Custom;
using Cwl.API.Processors;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Conditions;

[HarmonyPatch]
internal class SafeCreateConditionPatch
{
    internal static bool Prepare()
    {
        if (CwlConfig.SafeCreateClass) {
            TypeResolver.Add(ResolveCondition);
        }

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
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.InitStats))]
    internal static void OnInitStats(Chara __instance)
    {
        __instance.conditions.ForeachReverse(c => {
            if (c is not CustomCondition customCondition) {
                return;
            }

            __instance.conditions.Remove(customCondition);
            CwlMod.Log("cwl_log_post_cleanup".Loc(nameof(Condition), $"{customCondition.id} @ {__instance.id}"));
        });
    }

    [Time]
    private static Condition SafeCreateInvoke(string unqualified, string assembly, string alias)
    {
        try {
            var condition = ClassCache.Create<Condition>(unqualified, assembly);
            if (condition is null) {
                throw new SourceParseException("cwl_warn_deserialize");
            }

            return condition;
        } catch {
            // noexcept
        }

        CwlMod.Warn("cwl_warn_deserialize".Loc(nameof(Condition), alias, unqualified,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));

        var row = EMono.sources.stats.alias.TryGetValue(alias)!;
        row.name = "cwl_type_safety_cone".Loc(nameof(Condition), row.id, alias, unqualified);
        row.detail = "cwl_type_safety_desc".Loc();

        return new CustomCondition();
    }

    private static void ResolveCondition(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return;
        }

        if (objectType != typeof(Condition) || readType != typeof(object)) {
            return;
        }

        readType = typeof(CustomCondition);
        resolved = true;
        CwlMod.Warn("cwl_warn_deserialize".Loc(nameof(Condition), qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));
    }
}