using Erpc.Resources;
using HarmonyLib;

namespace Erpc.States;

[HarmonyPatch]
internal class UIStates
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LayerTitle), nameof(LayerTitle.OnInit))]
    internal static void OnTitleInstantiate()
    {
        ErpcMod.Session?.Update(new() {
            Details = "erpc_state_main_menu".Loc(),
            State = "erpc_state_embark".Loc(),
            Assets = new() {
                LargeImageKey = "default_app_banner",
                LargeImageText = ELayer.core.version.GetText(),
                SmallImageKey = "default_app_icon",
            },
        });
    }
}