using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.LangMod;
using Cwl.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomElement : Element
{
    private static bool _cleanup;
    private static HotItem? _held;

    internal static readonly Dictionary<int, SourceElement.Row> Managed = [];

    public static IEnumerable<SourceElement.Row> All => Managed.Values;

    internal void RemoveSafetyCone()
    {
        LayerAbility.SetDirty(this);
        pc.PlaySoundDrop();

        player.currentHotItem = null;
        player.RefreshCurrentHotItem();
    }

    [Time]
    public static void AddElement(SourceElement.Row r, string qualified)
    {
        try {
            var size = r.group switch {
                "FEAT" => 32,
                _ => 48,
            };
            ModSpriteReplacer.AppendSpriteSheet(r.alias, size, size);

            if (CwlConfig.QualifyTypeName) {
                r.type = qualified;
                CwlMod.Log("cwl_log_custom_type".Loc(nameof(Element), r.id, r.type));
            }

            Managed[r.id] = r;

            if (!_cleanup) {
                GameIOProcessor.AddSave(PurgeBeforeSave, false);
                GameIOProcessor.AddSave(RestoreAfterSave, true);
            }

            _cleanup = true;
        } catch {
            CwlMod.Error("cwl_error_qualify_type".Loc(nameof(Element), r.id, r.type));
            // noexcept
        }
    }

    // credits to 105gun
    [Time]
    internal static IEnumerator GainAbilityOnLoad()
    {
        if (!SafeSceneInitPatch.SafeToCreate) {
            yield break;
        }

        foreach (var element in All) {
            if (!element.tag.Contains("addEleOnLoad") ||
                player?.chara?.HasElement(element.id) is not false) {
                continue;
            }

            player.chara.GainAbility(element.id);
            CwlMod.Log("cwl_log_ele_gain".Loc(element.id, player.chara.Name));
        }
    }

    // credits to 105gun
    // https://github.com/105gun/ElinInduceVomiting/blob/master/ElementMissingWorkAround.cs
    [Time]
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