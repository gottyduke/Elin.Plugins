using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using TextureExpand.Textures;

namespace TextureExpand;

internal static class ModInfo
{
    internal const string Guid = "sobagaki.TextureExpand";
    internal const string Name = "TextureExpand";
    internal const string Version = "1.6.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class TextureExpand : BaseUnityPlugin
{
    private static readonly string[] _obsolete = [
        "seph.elin.textureexpand.genderfix",
        "seph.elin.textureexpand.npcreskinfix",
        "seph.elin.textureexpand.layerfix",
        "seph.elin.textureexpand.statusfix",
    ];

    internal static TextureExpand? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    public IEnumerator Start()
    {
        yield return null;
        UnpatchSuperseded();
    }

    public void OnStartCore()
    {
        TextureCache.Dir = Path.GetDirectoryName(Info.Location)!;

        try {
            TextureExpander.Initialize();
        } catch (Exception ex) {
            LogError($"failed to initialize\n{ex}");
        }
    }

    private static void UnpatchSuperseded()
    {
        var owners = new HashSet<string>();
        foreach (var method in Harmony.GetAllPatchedMethods()) {
            if (Harmony.GetPatchInfo(method) is { } info) {
                owners.UnionWith(info.Owners);
            }
        }

        foreach (var id in _obsolete) {
            if (!owners.Contains(id)) {
                continue;
            }

            try {
                Harmony.UnpatchID(id);
                LogWarning($"unpatched {id}, it should be unsubscribed");
            } catch (Exception ex) {
                LogError($"failed to unpatch {id}\n{ex}");
            }
        }
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void LogWarning(object payload)
    {
        Instance!.Logger.LogWarning(payload);
    }

    internal static void LogError(object payload)
    {
        Instance!.Logger.LogError(payload);
    }
}