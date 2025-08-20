using System;
using Cwl.Helper;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Recipes;

[HarmonyPatch]
internal class SafeCreateRecipePatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(RecipeManager), nameof(RecipeManager.Create))]
    internal static Exception? RethrowRecipeRebuild(Exception? __exception, RenderRow row, string type, string suffix)
    {
        if (__exception is null) {
            return null;
        }

        CwlMod.Warn<RecipeManager>("cwl_warn_recipe_rebuild".Loc(row.GetFieldValue("id"), $"{type}{suffix}"));

        // noexcept
        return null;
    }
}