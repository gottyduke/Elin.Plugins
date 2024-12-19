using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Quests;

[HarmonyPatch]
internal class SafeCreateQuestPatch
{
    private static readonly bool _cleanup = false;

    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("JsonSerializerInternalReader", "ResolveTypeName")]
    internal static IEnumerable<CodeInstruction> OnResolveExceptionIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(Type),
                    nameof(Type.IsAssignableFrom))),
                new CodeMatch(OpCodes.Brtrue))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldind_Ref),
                new CodeInstruction(OpCodes.Ldloca_S, (sbyte)5),
                new CodeInstruction(OpCodes.Ldarg_S, (sbyte)7),
                Transpilers.EmitDelegate(SafeResolveInvoke))
            .InstructionEnumeration();
    }

    [Time]
    private static bool SafeResolveInvoke(bool compatible, Type objectType, ref Type readType, string qualified)
    {
        if (compatible) {
            return true;
        }

        if (objectType != typeof(Quest) || readType != typeof(object)) {
            return false;
        }

        readType = typeof(Quest);
        CwlMod.Warn("cwl_warn_deserialize_quest".Loc(qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));

        if (!_cleanup) {
            CoroutineHelper.Deferred(PostLoadCleanup, () => !EClass.game.isLoading);
        }

        return true;
    }

    private static void PostLoadCleanup()
    {
    }
}