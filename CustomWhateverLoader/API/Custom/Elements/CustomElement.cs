using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using Cwl.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomElement : Element
{
    private static HotItem? _held;

    internal static readonly Dictionary<int, SourceElement.Row> Managed = [];

    public static IReadOnlyCollection<SourceElement.Row> All => Managed.Values;

    [Time]
    public static void AddElement(SourceElement.Row r, string qualified)
    {
        try {
            var size = r.group switch {
                nameof(FEAT) => 32,
                _ => 48,
            };
            SpriteReplacerHelper.AppendSpriteSheet(r.alias, size, size);

            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                if (!Managed.ContainsKey(r.id)) {
                    CwlMod.Log<CustomElement>("cwl_log_custom_type".Loc(nameof(Element), r.id, r.type));
                }
            }

            Managed[r.id] = r;
        } catch (Exception ex) {
            CwlMod.ErrorWithPopup<CustomElement>("cwl_error_qualify_type".Loc(nameof(Element), r.id, r.type), ex);
            // noexcept
        }
    }

    // credits to 105gun
    [Time]
    internal static void GainAbilityOnLoad()
    {
        if (!SafeSceneInitPatch.SafeToCreate) {
            return;
        }

        foreach (var (id, row) in sources.elements.map) {
            if (!row.tag.Contains("addEleOnLoad") ||
                player?.chara?.HasElement(id) is not false) {
                continue;
            }

            player.chara.AddElement(row);
        }
    }

    internal void RemoveSafetyCone()
    {
        LayerAbility.SetDirty(this);
        pc.PlaySoundDrop();

        player.currentHotItem = null;
        player.RefreshCurrentHotItem();
    }

    // credits to 105gun
    // https://github.com/105gun/ElinInduceVomiting/blob/master/ElementMissingWorkAround.cs
    [Time]
    [CwlPreSave]
    private static void PurgeBeforeSave(GameIOProcessor.GameIOContext context)
    {
        if (core?.game?.player?.currentHotItem is not HotItemAct act ||
            Managed.Keys.All(r => act.id != r)) {
            return;
        }

        _held = act;
        player.currentHotItem = null;
        player.RefreshCurrentHotItem();
    }

    [Time]
    [CwlPostSave]
    private static void RestoreAfterSave(GameIOProcessor.GameIOContext context)
    {
        if (core?.game?.player?.chara is null ||
            _held is null) {
            return;
        }

        player.currentHotItem = _held;
        player.RefreshCurrentHotItem();
        _held = null;
    }
}