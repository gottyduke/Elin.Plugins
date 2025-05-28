using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Processors;
using Cwl.Helper.Extensions;
using Cwl.Helper.Runtime;
using Cwl.Helper.Runtime.Exceptions;
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
                new(OpCodes.Ldelem_Ref),
                new OperandContains(OpCodes.Call, nameof(string.Concat)),
                new(OpCodes.Ldstr, "Elin"),
                new OperandContains(OpCodes.Call, nameof(ClassCache.Create)))
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_0),
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

            trait = (ClassCache.caches.dict[unqualified] = () => Activator.CreateInstance(qualified, false))() as Trait;

            if (_qualifiedTraits.Add(qualified.FullName)) {
                CwlMod.Log<Trait>("cwl_log_custom_type".Loc(nameof(Trait), unqualified, qualified.FullName));
            }
        } catch (Exception ex) {
            CwlMod.WarnWithPopup<Trait>("cwl_error_qualify_type".Loc(nameof(Trait), $"{unqualified} @ {owner.id}",
                ex.GetType().Name));
            // noexcept
        }

        return trait ?? new();
    }
}