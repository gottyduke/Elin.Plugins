using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
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
    internal const string Version = "0.9.16";

    [field: AllowNull]
    public static string BuildVersion => field ??= EmMod.Assembly.GetName().Version.ToString();
}

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
    }

    private void Start()
    {
#if !DEBUG
        Cwl.Helper.Exceptions.MonoFrame.AddVendorExclusion("Azure.");
        Cwl.Helper.Exceptions.MonoFrame.AddVendorExclusion("Microsoft.");
        Cwl.Helper.Exceptions.MonoFrame.AddVendorExclusion("OpenAI");
#endif

        EmConfig.InvalidateConfigs();
        EmConfig.EnableReloadWatcher();

#if EM_TEST_SERVICE
        ApiPoolSelector.MockTestServices();
#else
        ApiPoolSelector.Instance.LoadServices();
#endif

        EmKernel.RebuildKernel();

        EmPromptReset.EnablePromptWatcher();

        transform.GetOrCreate<EmScheduler>();
        transform.GetOrCreate<EmTalkTrigger>();
    }

    private void OnApplicationQuit()
    {
        ApiPoolSelector.Instance.SaveServices();

        Localizer.DumpUnlocalized();

        ExecutionAnalysis.DumpSessionActivities();
    }
}