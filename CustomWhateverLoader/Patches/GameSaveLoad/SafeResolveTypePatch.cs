using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Processors;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.GameSaveLoad;

[HarmonyPatch]
internal class SafeResolveTypePatch
{
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
    private static bool SafeResolveInvoke(bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return true;
        }

        TypeResolver.Resolve(ref resolved, objectType, ref readType, qualified);
        return resolved;
    }
}