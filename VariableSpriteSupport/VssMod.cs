using BepInEx;
using HarmonyLib;
using VSS.API;

namespace VSS;

internal static class ModInfo
{
    // for legacy reason...
    internal const string Guid = "dk.elinplugins.forcepixelsize";
    internal const string Name = "Variable Sprite Support";
    internal const string Version = "1.5";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
public class VssMod : BaseUnityPlugin
{
    internal static VssMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        var harmony = new Harmony(ModInfo.Guid);
        harmony.PatchAll();
    }

    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    public void Register(BaseUnityPlugin mod)
    {
        foreach (var declared in mod.GetType().Assembly.GetTypes()) {
            var dispatcher = AccessTools.FirstMethod(
                declared,
                mi => mi is { IsStatic: true, Name: "OnLayerRebuild" });
            if (dispatcher is null) {
                continue;
            }

            LayerRebuildDispatcher.AddDispatcher((layer, tex, index) => dispatcher.Invoke(null, [layer, tex, index]));
            Log($"registered layer rebuild dispatcher {mod.Info.Metadata.GUID}");
            break;
        }
    }
}