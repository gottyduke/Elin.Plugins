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
                new(OpCodes.Callvirt, AccessTools.Method(
                    typeof(Type),
                    nameof(Type.IsAssignableFrom))),
                new(OpCodes.Brtrue))
            .InsertAndAdvance(
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldind_Ref),
                new(OpCodes.Ldloca_S, (sbyte)5),
                new(OpCodes.Ldarg_S, (sbyte)7),
                Transpilers.EmitDelegate(SafeResolveInvoke))
            .InstructionEnumeration();
    }

    [Time]
    private static bool SafeResolveInvoke(bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return true;
        }

        if (readType != typeof(object)) {
            // type collision
            TypeResolver.WarnIncompatibleReadType(objectType, readType);
        }

        TypeResolver.Resolve(ref resolved, objectType, ref readType, qualified);

        return resolved;
    }
}