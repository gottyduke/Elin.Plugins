using System.Reflection;
using BepInEx;
using Cwl.Helper.Exceptions;
using HarmonyLib;
using ReflexCLI;

namespace Emmersive;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.emmersive";
    internal const string Name = "Elin Immersive Talks";
    internal const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class EmMod : BaseUnityPlugin
{
    internal static EmMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        EmConfig.Bind(Config);

        var assembly = Assembly.GetExecutingAssembly();
        CommandRegistry.assemblies.Add(assembly);
        Harmony.CreateAndPatchAll(assembly, ModInfo.Guid);

        transform.GetOrCreate<EmScheduler>();
    }

    private void Start()
    {
        MonoFrame.AddVendorExclusion("Microsoft.");
    }
}