global using System;
using BepInEx;
using Erpc.Resources;
using HarmonyLib;

namespace Erpc;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.discordrpc";
    internal const string Name = "Elin Rich Presence";
    internal const string Version = "1.0";
    internal const string AppId = "1305769942574170133";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class ErpcMod : BaseUnityPlugin
{
    internal static ErpcMod? Instance { get; private set; }
    internal static SessionManager? Session { get; private set; }

    private void Awake()
    {
        Instance = this;

        ErpcConfig.LoadConfig(Config);
        
        if (!LocHelper.LoadExternalLocs()) {
            return;
        }

        Session = new(ModInfo.AppId);
        Session.Initialize();

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    ~ErpcMod()
    {
        Session?.Dispose();
    }

    internal static void Log(object payload)
    {
        Instance?.Logger.LogInfo(payload);
#if DEBUG
        try {
            Msg.Say($"Erpc: {payload} ");
        } catch {
            // ignore
        }
#endif
    }
}