using System;
using System.Collections.Generic;
using System.Text;
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
    internal static readonly Dictionary<string, SerializableEffectData> CachedEffectData = [];

    [Time]
    [SwallowExceptions]
    [ConsoleCommand("load_effect_setting")]
    internal static string MergeEffectSetting()
    {
        Sprite[] sprites;
        try {
            sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        } catch {
            sprites = [];
        }

        var defaultSprite = Array.Find(sprites, s => s.name == "ranged_gun");
        var guns = Core.Instance.gameSetting.effect.guns;
        var sb = new StringBuilder();

        var effects = PackageIterator.GetJsonsFromPackage<SerializableEffectSetting>("Data/EffectSetting.guns.json");
        foreach (var (path, gunData) in effects) {
            foreach (var (id, read) in gunData) {
                try {
                    read.spriteId = read.spriteId.IsEmpty(read.idSprite);
                    var sprite = Array.Find(sprites, s => s.name == read.spriteId);

                    if (sprite == null) {
                        if (!SpriteReplacer.dictModItems.TryGetValue(read.spriteId, out var modded) ||
                            (sprite = modded.LoadSprite(name: read.spriteId)) == null) {
                            if ((sprite = Resources.Load<Sprite>(read.spriteId)) == null) {
                                sprite = defaultSprite;
                            }
                        }
                    }

                    GameSetting.EffectData data = new();
                    read.IntrospectCopyTo(data);
                    data.sprite = sprite;

                    guns[id] = data;
                    CachedEffectData[id] = read;

                    if (!read.caneColor.IsEmpty()) {
                        EClass.Colors.elementColors[id] = read.caneColor.Replace("0x", "").Replace("#", "").ToColor();
                    }

                    CwlMod.CurrentLoading = "cwl_log_effect_loaded".Loc(nameof(guns), id, path.ShortPath());
                    sb.AppendLine(CwlMod.CurrentLoading);
                    CwlMod.Log<DataLoader>(CwlMod.CurrentLoading);
                } catch (Exception ex) {
                    CwlMod.Error<DataLoader>("cwl_error_failure".Loc(ex.Message, ex));
                    // noexcept
                }
            }
        }

        return sb.ToString();
    }
}