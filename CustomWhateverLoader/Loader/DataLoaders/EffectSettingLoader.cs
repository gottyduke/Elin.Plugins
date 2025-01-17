using System;
using System.Collections;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using MethodTimer;
using UnityEngine;

namespace Cwl;

internal partial class DataLoader
{
    [Time]
    internal static IEnumerator MergeEffectSetting()
    {
        var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        var guns = Core.Instance.gameSetting.effect.guns;

        foreach (var gunData in PackageIterator.GetRelocatedFilesFromPackage("Data/EffectSetting.guns.json")) {
            if (!ConfigCereal.ReadConfig<SerializableEffectSetting>(gunData.FullName, out var effectSetting) ||
                effectSetting is null) {
                continue;
            }

            var path = gunData.ShortPath();
            CwlMod.CurrentLoading = $"[CWL] gun/{path}";

            foreach (var (id, read) in effectSetting) {
                try {
                    var sprite = Array.Find(sprites, s => s.name == read.spriteId);
                    if (sprite == null) {
                        if (!SpriteReplacer.dictModItems.TryGetValue(read.spriteId, out var modded) ||
                            (sprite = $"{modded}.png".LoadSprite(name: read.spriteId)) == null) {
                            continue;
                        }
                    }

                    GameSetting.EffectData data = new();
                    read.IntrospectCopyTo(data);
                    data.sprite = sprite;

                    guns[id] = data;

                    CwlMod.Log<DataLoader>("cwl_log_effect_loaded".Loc(nameof(guns), id, path));
                } catch {
                    // noexcept
                }
            }

            yield return null;
        }
    }
}