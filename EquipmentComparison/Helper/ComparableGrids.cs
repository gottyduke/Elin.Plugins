using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EC.Helper;

internal static class ComparableGrids
{
    internal static List<ButtonGridDrag> GetAllComparableGrids(this Chara owner, Thing item)
    {
        IEnumerable<Component> components;

        var petLayer = ELayer.ui.layers
            .OfType<LayerInventory>()
            .Where(l => l.Inv?.Chara is not null)
            .FirstOrDefault(l => l.Inv.Chara.IsPCFactionOrMinion || l.Inv.Chara == owner);

        if (petLayer is not null) {
            components = petLayer.invs
                .SelectMany(l => l.list.buttons)
                .Where(p => p.obj switch {
                    Thing { isEquipped: true } t
                        when !item.IsThrownWeapon && t.category.slot == item.category.slot => true,
                    Thing { isEquipped: false, IsThrownWeapon: true }
                        when item.IsThrownWeapon => true,
                    Thing { isEquipped: false, IsRangedWeapon: true }
                        when item.IsRangedWeapon => true,
                    _ => false,
                })
                .Select(p => p.component);
        } else {
            // in case users enabled "Unseal System Widgets" and disabled these, but why
            List<UIList.ButtonPair> grids = [
                ..WidgetEquip.Instance?.listMain.buttons ?? [],
                ..WidgetEquip.Instance?.listEtc.buttons ?? [],
                ..WidgetEquip.Instance?.transLayer.GetComponentInChildren<LayerInventory>()?.invs
                    .FirstOrDefault()?.list.buttons ?? [],
                ..WidgetCurrentTool.Instance?.list.buttons ?? [],
            ];

            components = grids
                .Where(p => p.obj switch {
                    BodySlot { thing.isEquipped: true } s
                        when s.elementId == item.category.slot => true,
                    Thing { isEquipped: false, IsThrownWeapon: true }
                        when item.IsThrownWeapon => true,
                    Thing { isEquipped: false, IsRangedWeapon: true }
                        when item.IsRangedWeapon => true,
                    _ => false,
                })
                .Select(p => p.component);
        }

        return components
            .OfType<ButtonGridDrag>()
            .Where(b => b.card != item)
            .ToList();
    }
}