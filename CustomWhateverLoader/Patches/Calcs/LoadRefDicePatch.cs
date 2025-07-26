using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Calcs;

[HarmonyPatch]
internal class LoadRefDicePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Dice), nameof(Dice.Create), typeof(string), typeof(int), typeof(Card), typeof(Act))]
    internal static void OnReplaceNullDice(ref string id, Act? act)
    {
        if (act?.source is not { } source) {
            return;
        }

        if (!source.tag.Contains("addDice") &&
            // enable ref dice for all custom elements
            !CustomElement.Managed.ContainsKey(source.id)) {
            return;
        }

        if (EMono.sources.calc.map.ContainsKey(source.alias)) {
            id = source.alias;
        }
    }
}