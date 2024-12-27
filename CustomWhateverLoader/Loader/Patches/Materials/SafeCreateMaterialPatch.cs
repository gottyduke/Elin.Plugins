using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Loader.Patches.Materials;

[HarmonyPatch]
internal class SafeCreateMaterialPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElementContainer), nameof(ElementContainer.ApplyMaterialElementMap))]
    internal static void SafeApplyInvoke(ElementContainer __instance, Thing t)
    {
        try {
            ref var map = ref EMono.sources.materials.map;
            if (map.ContainsKey(t.idMaterial)) {
                return;
            }

            map.TryAdd(t.idMaterial, map[0]);
        } catch {
            // noexcept
        }

        CwlMod.Warn("cwl_warn_deserialize".Loc(nameof(SourceMaterial), t.idMaterial, nameof(SourceMaterial.Row),
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));
    }
}