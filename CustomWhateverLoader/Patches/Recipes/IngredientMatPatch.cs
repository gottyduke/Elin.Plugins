using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Recipes;

//[HarmonyPatch]
internal class IngredientMatPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(RecipeSource), nameof(RecipeSource.GetIngredients))]
    internal static IEnumerable<CodeInstruction> OnInstantiatingIngredientIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(
                    typeof(Recipe.Ingredient))))
            .Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldelem_Ref),
                Transpilers.EmitDelegate(MutateIngredient))
            .InstructionEnumeration();
    }

    private static Recipe.Ingredient MutateIngredient(Recipe.Ingredient ingredient, string component)
    {
        var item = component.Split("|")[0];
        var spec = item.Parse("/", 3);
        if (spec[2] is not null) {
            ingredient.mat = ReverseId.Material(spec[2]!);
        }

        return ingredient;
    }
}