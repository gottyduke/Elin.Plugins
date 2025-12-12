using System;
using System.Collections.Generic;
using System.IO;
using Cwl.API;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using MethodTimer;
using Newtonsoft.Json;
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
        CachedEffectData.Clear();

        Sprite[] sprites;
        try {
            sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        } catch {
            sprites = [];
        }

        var defaultSprite = Array.Find(sprites, s => s.name == "ranged_gun");
        var guns = Core.Instance.gameSetting.effect.guns;
        using var sb = StringBuilderPool.Get();

        var effects = PackageIterator.GetJsonsFromPackage<SerializableEffectSetting>("Data/EffectSetting.guns.json");
        foreach (var (path, gunData) in effects) {
            foreach (var (id, read) in gunData) {
                try {
                    read.spriteId = read.spriteId.OrIfEmpty(read.idSprite);
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

    [ConsoleCommand("dump_guns")]
    internal static string DumpGuns()
    {
        var guns = EClass.setting.effect.guns;
        var path = $"{CorePath.rootExe}/guns.json";

        Dictionary<string, SerializableEffectData> effects = [];
        foreach (var (id, gun) in guns) {
            var data = new SerializableEffectData();
            gun.IntrospectCopyTo(data);
            effects[id] = data;
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(effects, Formatting.Indented, ConfigCereal.Settings));
        return $"dumped {effects.Count} guns data to {path}";
    }
}