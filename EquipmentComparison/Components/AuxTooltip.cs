using System;
using System.Collections.Generic;
using System.Linq;
using EC.Helper;
using HarmonyLib;
using UnityEngine;

namespace EC.Components;

internal class AuxTooltip : MonoBehaviour
{
    private readonly List<UITooltip> _cached = [];
    internal UITooltip? BaseNote { get; private set; }
    internal bool IsEnabled { get; private set; }

    private void Awake()
    {
        var instance = GetComponent<TooltipManager>();
        BaseNote = instance.tooltips.FirstOrDefault(t => t.name == "note");
        if (BaseNote == null) {
            EcMod.Log("failed to patch aux notes, missing base note");
            return;
        }

        if (instance.tooltips.Any(t => t.name.StartsWith("aux"))) {
            EcMod.Log("already patched aux notes");
            return;
        }

        _cached.Clear();
        var maxNotes = EquipmentComparisonConfig.MaxAuxNotes!.Value;
        for (var i = 0; i < maxNotes; ++i) {
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

        IsEnabled = true;
        EcMod.Log("aux notes patched");
    }

    private void Update()
    {
        var mod = EquipmentComparisonConfig.Modifier!.Value;
        if (mod != KeyCode.None && !Input.GetKey(mod)) {
            return;
        }

        var toggle = EquipmentComparisonConfig.Toggle!.Value;
        if (toggle == KeyCode.None || !Input.GetKeyDown(toggle)) {
            return;
        }

        IsEnabled = !IsEnabled;
        if (!IsEnabled) {
            _cached.Do(n => n.SetActive(false));
        }

        EClass.pc.Say(Loc.TogglePrompt(IsEnabled));
    }

    internal static void TryDrawAuxTooltip(UIButton? btn)
    {
        // stop all renderings
        var tm = TooltipManager.Instance;
        var @this = tm.gameObject.GetOrAddComponent<AuxTooltip>();
        var notes = @this.GetComponentsInChildren<AuxNote>();
        notes.Do(n => n.SetActive(false));

        if (!@this.IsEnabled) {
            return;
        }

        if (btn is not ButtonGridDrag { card: Thing thing } grid) {
            return;
        }

        if (btn is ButtonHotItem ||
            grid.invOwner is InvOwnerEquip or InvOwnerHotbar) {
            // not drawing for hotbar item
            return;
        }

        if (thing is { IsEquipmentOrRanged: false, IsThrownWeapon: false } ||
            thing.isEquipped) {
            // already equipped or not a equipment
            return;
        }

        // unless checking pet inv, always compare with pc
        var owner = grid.invOwner.Chara?.IsPCFactionOrMinion ??
                    false
            ? grid.invOwner.Chara
            : EClass.pc;

        // GetEquippedThing() only returns first equipped
        // need to iterate in case of dual wielding
        // also search for hotbar & toolbelt items
        var maxNotes = EquipmentComparisonConfig.MaxAuxNotes!.Value;
        var comparables = GetAllComparableGrids(thing, owner);
        for (var i = 0; i < Math.Min(maxNotes, comparables.Count); ++i) {
            var copyTooltip = comparables[i].CopyTooltipWithId($"aux_note_{i}");
            tm.ShowTooltip(copyTooltip, @this.BaseNote!.transform);
        }
    }

    private static List<ButtonGridDrag> GetAllComparableGrids(Thing item, Chara owner)
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
                BodySlot { thing: not null } s
                    when s.elementId == item.category.slot => true,
                Thing { IsThrownWeapon: true }
                    when item.IsThrownWeapon => true,
                _ => false,
            })
            .Select(p => p.component)
            .OfType<ButtonGridDrag>()
            .Where(b => b.card != item)
            .ToList();
    }

    internal static void SetTooltipOverride(Card card, UITooltip tooltip)
    {
        card.WriteNote(tooltip.note, n => {
            var headerText = n.GetComponentInChildren<UIText>();
            headerText.text = $"{Loc.EquippedIndicator} {headerText.text}";
        });
    }
}