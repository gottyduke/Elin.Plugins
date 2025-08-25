using System;
using System.Linq;
using Cwl.Helper.Unity;
using MethodTimer;
using ReflexCLI.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cwl;

internal partial class DataLoader
{
    internal const string EffectPathEntry = "Media/Effect/General";
    internal static Effect? EffectTemplate { get; private set; }

    internal static bool RelocateEffect(string path, ref Object? loaded)
    {
        if (!path.StartsWith(EffectPathEntry, StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        if (EffectTemplate == null) {
            CwlMod.Warn<DataLoader>($"effect template {EffectTemplate} is null");
            return false;
        }

        var effectId = path.Split('/')[^1];
        var effectSheet = effectId.LoadSprite();
        if (effectSheet == null) {
            return false;
        }

        var effect = Object.Instantiate(EffectTemplate);
        effect.name = effectId;
        effect.sprites = effectSheet.SliceSprite(effectId).ToArray();
        Object.DontDestroyOnLoad(effect);

        CwlMod.Log<DataLoader>($"loaded effect {effectId}, {effect.sprites.Length} frames");

        loaded = effect;
        return true;
    }

    [Time]
    [SwallowExceptions]
    internal static void SetupEffectTemplate()
    {
        if (EffectTemplate != null) {
            Object.Destroy(EffectTemplate);
        }

        EffectTemplate = Resources.Load<Effect>($"{EffectPathEntry}/rod");
        if (EffectTemplate == null) {
            CwlMod.Warn<DataLoader>($"effect template {EffectTemplate} is null");
            return;
        }

        EffectTemplate.name = "cwl_template_effect";
    }

    [ConsoleCommand("clear_effect_cache")]
    private static void ClearLoadedEffects()
    {
        var manager = Effect.manager;
        if (manager == null) {
            return;
        }

        manager.list.Clear();
        manager.effects.list.Clear();
        manager.effects.map.Clear();
    }
}