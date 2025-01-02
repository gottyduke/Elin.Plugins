﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Traits;

[HarmonyPatch]
internal class SafeCreateTraitPatch
{
    private static readonly HashSet<string> _qualifiedTraits = [];

    internal static bool Prepare()
    {
        return CwlConfig.QualifyTypeName;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Card), nameof(Card.ApplyTrait))]
    internal static IEnumerable<CodeInstruction> OnCacheCreateIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldelem_Ref),
                new CodeMatch(o => o.opcode == OpCodes.Call &&
                                   o.operand.ToString().Contains(nameof(string.Concat))),
                new CodeMatch(OpCodes.Ldstr, "Elin"),
                new CodeMatch(o => o.opcode == OpCodes.Call &&
                                   o.operand.ToString().Contains(nameof(ClassCache.Create))))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                Transpilers.EmitDelegate(SafeCreateInvoke))
            .RemoveInstruction()
            .InstructionEnumeration();
    }

    private static Trait SafeCreateInvoke(string unqualified, string assembly, Card owner, bool transformed = false)
    {
        Trait? trait = null;
        try {
            if (!transformed) {
                var traitName = unqualified;
                TraitTransformer.Transform(ref traitName, owner);
                return SafeCreateInvoke(traitName, assembly, owner, true);
            }

            trait = ClassCache.Create<Trait>(unqualified, assembly);
            if (trait is not null) {
                return trait;
            }

            var qualified = TypeQualifier.TryQualify<Trait>(unqualified);
            if (qualified?.FullName is null) {
                throw new SourceParseException("cwl_error_qualify_type");
            }

            ClassCache.caches.dict[unqualified] = () => Activator.CreateInstance(qualified);
            trait = ClassCache.caches.dict[unqualified]() as Trait;

            if (_qualifiedTraits.Add(qualified.FullName)) {
                CwlMod.Log("cwl_log_custom_type".Loc(nameof(Trait), unqualified, qualified.FullName));
            }
        } catch {
            CwlMod.Warn("cwl_error_qualify_type".Loc(nameof(Trait), $"{unqualified} @ {owner.id}", ""));
            // noexcept
        }

        return trait ?? new();
    }
}