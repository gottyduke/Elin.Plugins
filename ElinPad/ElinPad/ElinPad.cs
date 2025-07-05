using BepInEx;
using ElinPad.Components;
using HarmonyLib;

namespace ElinPad;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.elinpad";
    internal const string Name = "Elin With Controller";
    internal const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal partial class ElinPad : BaseUnityPlugin
{
    internal static ElinPad? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        EpConfig.Load(Config);

        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();

        gameObject.AddComponent<PadController>();
        gameObject.AddComponent<PadEventManager>();

        if (EpConfig.ShowTrackInput) {
            gameObject.AddComponent<PadTrackInput>();
        }
    }
}