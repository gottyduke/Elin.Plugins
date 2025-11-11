using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using Cwl.Helper.String;
using ElinTogether.Helper;
using ElinTogether.Net;
using HarmonyLib;
using ReflexCLI;
using Steamworks;

namespace ElinTogether;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.elintogether";
    internal const string Name = "Elin Together";
    internal const string Version = "0.8.17";

    [field: AllowNull]
    public static string BuildVersion => field ??= EmpMod.Assembly.GetName().Version.ToString();
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed class EmpMod : BaseUnityPlugin
{
    internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    internal static readonly Harmony SharedHarmony = new(ModInfo.Guid);

    internal static EmpMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        EmpPop.InitLogger();
        EmpConfig.Bind();

        CommandRegistry.assemblies.Add(Assembly);

#if DEBUG
        SharedHarmony.PatchAll(Assembly);
#endif
    }

    private void Start()
    {
#if !DEBUG
        MonoFrame.AddVendorExclusion("MessagePack.");
#endif

        ResourceFetch.InvalidateTemp();

        SteamNetworkingUtils.InitRelayNetworkAccess();

        //var _ = ProgressIndicator.CreateProgress(() => new(Build()), _ => false, 1f);
    }

    private void OnApplicationQuit()
    {
        NetSession.Instance.RemoveComponent();
        StringAllocator.UnpinSharedStringHandles();
    }

    private string Build()
    {
        using var sb = StringBuilderPool.Get();

        if (EClass.core.IsGameStarted) {
            var ai = EClass.pc.ai;
            sb.Append(ai.ToString());
        }

        return sb.ToString();
    }
}