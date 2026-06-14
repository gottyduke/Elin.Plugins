using System.Reflection;
using BepInEx;
using ElinTogether.Helper;
using ElinTogether.Helper.String;
using ElinTogether.Net;
using ElinTogether.Patches;
using HarmonyLib;
using ReflexCLI;
using Steamworks;

namespace ElinTogether;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.elintogether";
    internal const string Name = "Elin Together";
    internal const string Version = "0.17.0";

    internal static string BuildVersion => field ??= EmpMod.Assembly.GetName().Version.ToString();
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
        EModding.Helper.Runtime.Exceptions.MonoFrame.AddVendorExclusion("MessagePack.");
#endif

        ResourceFetch.InvalidateTemp();
        EmpConfig.EnableReloadWatcher();

        SteamNetworkingUtils.InitRelayNetworkAccess();

        NetSession.Instance.Lobby.TryParseLobbyCommand();

        TitleButtonPatch.RegisterTitleButton(Scene.Mode.Title);
    }

    private void OnApplicationQuit()
    {
        NetSession.Instance.RemoveComponent();
        StringAllocator.UnpinSharedStringHandles();
    }
}