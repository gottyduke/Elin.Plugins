using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Sources;

//[HarmonyPatch]
internal class SafeParseElementPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Core), nameof(Core.ParseElements))]
    internal static bool SafeParseElements(string str, ref int[] __result)
    {
        if (str.IsEmptyOrNull) {
            __result = [];
            return false;
        }

        var pairs = str.Replace("\n", "").Split(',');
        __result = new int[pairs.Length * 2];
        for (var i = 0; i < pairs.Length; ++i) {
            var element = pairs[i].Split('/');
            __result[i * 2] = Core.GetElement(element[0]);
            __result[i * 2 + 1] = element.Length > 1 && int.TryParse(element[1], out var value) ? value : 1;
        }
        return false;
    }
}