using Cwl.Helper.Unity;
using HarmonyLib;

namespace Cwl.Patches.UIs;

[HarmonyPatch]
internal class ContextSystemEvent
{
    [SwallowExceptions]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HotItemContext), nameof(HotItemContext.Show))]
    internal static void OnShowSystemMenu(string id)
    {
        if (id != "system") {
            return;
        }

        var context = UIContextMenuManager.Instance.currentMenu;
        if (context == null) {
            return;
        }

        if (ContextMenuHelper.EntryProxies.Count == 0) {
            return;
        }

        var mods = context.AddChild("Mods");
        foreach (var proxy in ContextMenuHelper.EntryProxies) {
            PopulateMenu(mods, proxy);
        }

        foreach (var popper in context.GetComponentsInChildren<UIContextMenuPopper>()) {
            if (popper.id != "etc") {
                continue;
            }

            mods.transform.parent.SetSiblingIndex(popper.transform.GetSiblingIndex());
            break;
        }
    }

    [SwallowExceptions]
    private static void PopulateMenu(UIContextMenu parent, ContextMenuHelper.ContextMenuProxy proxy)
    {
        if (proxy.IsPopper) {
            var popper = parent.AddChild(proxy.DisplayName);
            foreach (var child in proxy.Children) {
                PopulateMenu(popper, child);
            }
        } else {
            parent.AddButton(proxy.DisplayName, proxy.OnClick);
        }
    }
}