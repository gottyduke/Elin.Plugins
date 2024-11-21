using System.Collections.Generic;
using System.Linq;

namespace EC.Helper;

internal static class ComparableGrids
{
    internal static List<ButtonGridDrag> GetAllComparableGrids(this Chara owner, Thing item)
    {
        // wtf did I write
        if (!owner.IsPCC) {
            return ELayer.ui.layers
                .OfType<LayerInventory>()
                .Where(l => l.Inv.Chara == owner)
                .SelectMany(l => l.invs)
                .SelectMany(l => l.list.buttons)
                .Where(p => p.obj switch {
                    Thing { isEquipped: true } t
                        when !item.IsThrownWeapon &&
                             t.category.slot == item.category.slot => true,
                    Thing { isEquipped: false, IsThrownWeapon: true }
                        when item.IsThrownWeapon => true,
                    Thing { isEquipped: false, IsRangedWeapon: true }
                        when item.IsRangedWeapon => true,
                    _ => false,
                })
                .Select(p => p.component)
                .OfType<ButtonGridDrag>()
                .Where(b => b.card != item)
                .ToList();
        }

        List<UIList.ButtonPair> grids = [
            ..WidgetEquip.Instance.listMain.buttons,
            ..WidgetEquip.Instance.listEtc.buttons,
            ..WidgetEquip.Instance.transLayer.GetComponentInChildren<LayerInventory>().invs
                .FirstOrDefault()?.list.buttons ?? [],
            ..WidgetCurrentTool.Instance.list.buttons,
        ];

        return grids
            .Where(p => p.obj switch {
                BodySlot { thing.isEquipped: true } s
                    when s.elementId == item.category.slot => true,
                Thing { isEquipped: false, IsThrownWeapon: true }
                    when item.IsThrownWeapon => true,
                Thing { isEquipped: false, IsRangedWeapon: true }
                    when item.IsRangedWeapon => true,
                _ => false,
            })
            .Select(p => p.component)
            .OfType<ButtonGridDrag>()
            .Where(b => b.card != item)
            .ToList();
    }
}