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
        var apiPool = ApiPoolSelector.Instance;
        var (_, keys) = PackageIterator
            .GetJsonFromPackage<Dictionary<string, string[]>>("Emmersive/DebugKeys.json", ModInfo.Guid);

        if (keys is not null) {
            foreach (var key in keys["Em_GoogleGeminiAPI_Dummy"]) {
                apiPool.AddService(new GoogleProvider(key) {
                    CurrentModel = "gemini-2.5-flash",
                });
            }

            foreach (var key in keys["Em_DeepSeekAPI_Dummy"]) {
                apiPool.AddService(new OpenAIProvider(key) {
                    EndPoint = "https://api.deepseek.com/v1",
                    Alias = "DeepSeek",
                    CurrentModel = "deepseek-chat",
                });
            }

            foreach (var key in keys["Em_OpenAIAPI_Dummy"]) {
                apiPool.AddService(new OpenAIProvider(key) {
                    CurrentModel = "gpt-5-nano",
                });
            }
        }
#else
        ApiPoolSelector.Instance.LoadServices();
#endif

        EmKernel.RebuildKernel();
    }

    private void OnApplicationQuit()
    {
        ApiPoolSelector.Instance.SaveServices();
        Localizer.DumpUnlocalized();
    }
}