using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using ReflexCLI;

namespace Cwl;

public static class ModInfo
{
    // for legacy reason
#if NIGHTLY
    public const string TargetVersion = "nightly";
    public const string InternalGuid = Guid;
#else
    public const string TargetVersion = "stable";
    public const string InternalGuid = $"{Guid}.{TargetVersion}";
#endif
    public const string Guid = "dk.elinplugins.customdialogloader";
    public const string Name = "Custom Whatever Loader";

    public const string Version = "1.20.50";

    // for runtime versions
    [field: MaybeNull]
    public static string BuildVersion => field ??= Assembly.GetExecutingAssembly().GetName().Version.ToString();
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class CwlMod : BaseUnityPlugin
{
    internal static readonly Harmony SharedHarmony = new(ModInfo.Guid);
    internal static CwlMod? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        CwlConfig.Load(Config);

        CommandRegistry.assemblies.Add(Assembly.GetExecutingAssembly());

        LoadLoc();
        BuildPatches();
    }

    private void OnDisable()
    {
        ExecutionAnalysis.DispatchAnalysis();
    }
}