using System.Collections.Generic;
using System.Linq;
using SwallowExceptions.Fody;
using UnityEngine;

namespace EC.Helper;

internal static class ComparableGrids
{
    extension(Chara owner)
    {
        [SwallowExceptions]
        internal List<ButtonGridDrag> GetAllComparableGrids(Thing item)
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
                List<UIList.ButtonPair> grids = [];
                // in case users enabled "Unseal System Widgets" and disabled these, but why
                var equip = WidgetEquip.Instance;
                if (equip != null) {
                    if (equip.listMain != null) {
                        grids.AddRange(equip.listMain.buttons);
                    }

                    if (equip.listEtc != null) {
                        grids.AddRange(equip.listEtc.buttons);
                    }

                    var inv = equip.transLayer.GetComponentInChildren<LayerInventory>();
                    if (inv != null) {
                        grids.AddRange(inv.invs.FirstOrDefault()?.list.buttons ?? []);
                    }
                }

                var toolbelt = WidgetCurrentTool.Instance;
                if (toolbelt != null) {
                    grids.AddRange(toolbelt.list.buttons);
                }

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
}