using System.Reflection;
using BepInEx;
using Cwl.Helper.Exceptions;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.Helper;
using HarmonyLib;
using ReflexCLI;

namespace Emmersive;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.emmersive";
    internal const string Name = "Elin with AI (Beta)";
    internal const string Version = "0.9.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class EmMod : BaseUnityPlugin
{
    internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    internal static EmMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        EmConfig.Bind(Config);
        CommandRegistry.assemblies.Add(Assembly);
        Harmony.CreateAndPatchAll(Assembly, ModInfo.Guid);

        transform.GetOrCreate<EmScheduler>();
    }

    private void Start()
    {
        MonoFrame.AddVendorExclusion("Azure.");
        MonoFrame.AddVendorExclusion("Microsoft.");
        MonoFrame.AddVendorExclusion("OpenAI");

#if EM_TEST_SERVICE
        ApiPoolSelector.MockTestServices();
#else
        ApiPoolSelector.Instance.LoadServices();
#endif

        EmKernel.RebuildKernel();
    }

    private void OnApplicationQuit()
    {
        ApiPoolSelector.Instance.SaveServices();
        Localizer.DumpUnlocalized();
        ExecutionAnalysis.DumpSessionActivities();
    }
}