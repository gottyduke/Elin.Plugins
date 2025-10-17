using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using Cwl.Helper.Exceptions;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.Helper;
using HarmonyLib;
using ReflexCLI;
using ReflexCLI.Attributes;

namespace Emmersive;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.emmersive";
    internal const string Name = "Elin with AI (Beta)";
    internal const string Version = "0.9.4";

    [field: AllowNull]
    public static string BuildVersion => field ??= EmMod.Assembly.GetName().Version.ToString();
}

[ConsoleCommandClassCustomizer("em")]
[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed partial class EmMod : BaseUnityPlugin
{
    internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    internal static EmMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        EmConfig.Bind();

        CommandRegistry.assemblies.Add(Assembly);
        Harmony.CreateAndPatchAll(Assembly, ModInfo.Guid);

        transform.GetOrCreate<EmScheduler>();
    }

    private void Start()
    {
        MonoFrame.AddVendorExclusion("Azure.");
        MonoFrame.AddVendorExclusion("Microsoft.");
        MonoFrame.AddVendorExclusion("OpenAI");

        EmConfig.InvalidateConfigs();
        EmConfig.EnableReloadWatcher();

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
        ApiPoolSelector.Instance.CleanServiceParams();

        Localizer.DumpUnlocalized();

        ResourceFetch.SaveActiveResources();

        ExecutionAnalysis.DumpSessionActivities();
    }
}