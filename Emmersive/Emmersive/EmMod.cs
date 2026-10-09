using System;
using System.Reflection;
using BepInEx;
using Emmersive.API.Services;
using Emmersive.API.ThirdParty;
using Emmersive.Components;
using Emmersive.Helper;
using EModding.Helper.Runtime.Exceptions;
using HarmonyLib;
using ReflexCLI;

namespace Emmersive;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.emmersive";
    internal const string Name = "Elin with AI (Beta)";
    internal const string Version = "0.11.0";

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

        var harmony = new Harmony(ModInfo.Guid);
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly)) {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) {
                continue;
            }

            try {
                harmony.CreateClassProcessor(type).Patch();
            } catch (Exception ex) {
                Error<EmMod>($"failed to apply {type.Name}\n{ex}");
            }
        }
    }

    private void Start()
    {
#if !DEBUG
        MonoFrame.AddVendorExclusion("Azure.");
        MonoFrame.AddVendorExclusion("Microsoft.");
        MonoFrame.AddVendorExclusion("OpenAI");
#endif

        EmConfig.InvalidateConfigs();
        EmConfig.EnableReloadWatcher();

        try {
#if EM_TEST_SERVICE
            ApiPoolSelector.MockTestServices();
#else
            ApiPoolSelector.Instance.LoadServices();
#endif

            EmKernel.RebuildKernel();
        } catch (Exception ex) {
            ErrorWithPopup<EmMod>("em_ui_err_startup".lang(), ex);
        }

        EmPromptReset.EnablePromptWatcher();

        transform.GetOrCreate<EmScheduler>();

        BaseModManager.PublishEvent(EmEvent.EmmersiveReady, EmEvent.ApiLevel);
    }

    private void OnApplicationQuit()
    {
        ApiPoolSelector.Instance.SaveServices();

        Localizer.DumpUnlocalized();

        ExecutionAnalysis.DumpSessionActivities();
    }
}