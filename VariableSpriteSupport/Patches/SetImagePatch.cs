using HarmonyLib;

namespace VSS.Patches;

[HarmonyPatch]
internal class SetImagePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemGeneral), nameof(ItemGeneral.SetChara), typeof(Chara))]
    internal static void OnSetImage(ItemGeneral __instance, Chara c)
    {
        if (!c.IsPCC) {
            return;
        }

        var sprite = __instance.button1.icon.sprite;
        var rect = __instance.button1.icon.rectTransform;

        var offsetHeight = sprite.textureRect.height - 48;
        rect.position = rect.position with { y = rect.position.y + offsetHeight };
    }
}