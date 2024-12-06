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
    private bool IsEnabled { get; set; }

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
        var maxNotes = EcConfig.MaxAuxNotes!.Value;
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
        var mod = EcConfig.Modifier!.Value;
        if (mod != KeyCode.None && !Input.GetKey(mod)) {
            return;
        }

        var toggle = EcConfig.Toggle!.Value;
        if (toggle == KeyCode.None || !Input.GetKeyDown(toggle)) {
            return;
        }

        IsEnabled = !IsEnabled;
        if (!IsEnabled) {
            _cached.Do(n => n.SetActive(false));
        }

        EClass.pc.Say(EcLoc.TogglePrompt(IsEnabled));
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
            // already equipped or not an equipment
            return;
        }

        // unless checking pet inv, always compare with pc
        var owner = grid.invOwner?.Chara?.IsPCFactionOrMinion ?? false
            ? grid.invOwner.Chara
            : EClass.pc;

        // GetEquippedThing() only returns first equipped
        // need to iterate in case of dual wielding
        // also search for hotbar & toolbelt items
        var maxNotes = EcConfig.MaxAuxNotes!.Value;
        var comparables = owner.GetAllComparableGrids(thing);
        for (var i = 0; i < Math.Min(maxNotes, comparables.Count); ++i) {
            var comparable = comparables[i];
            var header = comparable.card.IsRangedWeapon
                ? EcLoc.CarriedIndicator
                : EcLoc.EquippedIndicator;
            var copyTooltip = comparable.CopyTooltipWithId($"aux_note_{i}", header);
            tm.ShowTooltip(copyTooltip, @this.BaseNote!.transform);
        }
    }
}