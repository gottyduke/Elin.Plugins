using System;
using System.Collections.Generic;
using System.Linq;
using Ec.Helper;
using HarmonyLib;
using UnityEngine;

namespace EC.Components;

internal class AuxTooltip : MonoBehaviour
{
    private readonly List<UITooltip> _cached = [];
    internal UITooltip? BaseNote { get; private set; }

    private void Awake()
    {
        var instance = GetComponent<TooltipManager>();
        BaseNote = instance.tooltips.FirstOrDefault(t => t.name == "note");
        if (BaseNote == null) {
            ECMod.Log("failed to patch aux notes, missing base note");
            return;
        }

        if (instance.tooltips.Any(t => t.name.StartsWith("aux"))) {
            ECMod.Log("already patched aux notes");
            return;
        }

        _cached.Clear();
        for (var i = 0; i < 2; ++i) {
            var aux = Instantiate(BaseNote, instance.transform);
            aux.gameObject.AddComponent<AuxNote>();
            aux.name = $"aux_note_{i}";
            aux.followType = UITooltip.FollowType.None;
            aux.SetActive(false);
            _cached.Add(aux);
        }

        instance.tooltips = [
            ..instance.tooltips,
            .._cached,
        ];
        ECMod.Log("aux notes patched");
    }

    internal static void TryDrawAuxTooltip(UIButton? btn)
    {
        // stop all renderings
        var tm = TooltipManager.Instance;
        var @this = tm.gameObject.GetOrAddComponent<AuxTooltip>();
        var notes = @this.GetComponentsInChildren<AuxNote>();
        notes.Do(n => n.SetActive(false));

        if (btn is not ButtonGrid { card: Thing thing } inv) {
            return;
        }

        if (!thing.IsEquipmentOrRanged ||
            thing.isEquipped) {
            // already equipped or not a equipment
            return;
        }

        // unless checking pet inv, always compare with pc
        var owner = inv.invOwner.Chara.IsPCFactionOrMinion
            ? inv.invOwner.Chara
            : EClass.pc;

        // GetEquippedThing() only returns first equipped
        // need to iterate in case of dual wielding
        // also search for hotbar & toolbelt items
        var comparables = GetAllComparableGrids(thing.category.slot, owner);
        if (comparables.Count == 0) {
            // nothing to compare
            return;
        }

        for (var i = 0; i < Math.Min(2, comparables.Count); ++i) {
            var copyTooltip = comparables[i].tooltip.CopyWithId($"aux_note_{i}");
            tm.ShowTooltip(copyTooltip, @this.BaseNote!.transform);
        }
    }

    private static List<ButtonGridDrag> GetAllComparableGrids(int slotId, Chara owner)
    {
        // wtf did I write
        if (!owner.IsPCC) {
            return ELayer.ui.layers
                .OfType<LayerInventory>()
                .Where(l => l.Inv.Chara == owner)
                .SelectMany(l => l.invs)
                .SelectMany(l => l.list.buttons)
                .Where(p => p.obj is Thing { isEquipped: true } t &&
                            t.category.slot == slotId)
                .Select(p => p.component)
                .OfType<ButtonGridDrag>()
                .ToList();
        }

        List<UIList.ButtonPair> grids = [
            ..WidgetEquip.Instance.listMain.buttons,
            ..WidgetEquip.Instance.listEtc.buttons,
            ..WidgetEquip.Instance.transLayer.GetComponentInChildren<LayerInventory>().invs.FirstOrDefault()?.list
                .buttons ?? [],
            ..WidgetCurrentTool.Instance.list.buttons,
        ];

        return grids
            .Where(p => p.obj is BodySlot { thing: not null } s &&
                        s.elementId == slotId)
            .Select(p => p.component)
            .OfType<ButtonGridDrag>()
            .ToList();
    }
}