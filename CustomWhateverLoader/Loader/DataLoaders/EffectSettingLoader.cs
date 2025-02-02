using System;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using MethodTimer;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl;

internal partial class DataLoader
{
    [Time]
    [ConsoleCommand("load_effect_setting")]
    internal static void MergeEffectSetting()
    {
        var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        var guns = Core.Instance.gameSetting.effect.guns;

        var effects = PackageIterator.GetRelocatedJsonsFromPackage<SerializableEffectSetting>("Data/EffectSetting.guns.json");
        foreach (var (path, gunData) in effects) {
            foreach (var (id, read) in gunData) {
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

                    CwlMod.Log<DataLoader>("cwl_log_effect_loaded".Loc(nameof(guns), id, path.ShortPath()));
                } catch {
                    // noexcept
                }
            }
        }
    }
}