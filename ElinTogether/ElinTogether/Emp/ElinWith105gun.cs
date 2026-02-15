using System.Reflection;
using BepInEx;
using Cwl.API.Attributes;
using ElinTogether.Helper;
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
    internal const string Version = "0.9.4";

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
        Cwl.Helper.Exceptions.MonoFrame.AddVendorExclusion("MessagePack.");
#endif

        ResourceFetch.InvalidateTemp();

        SteamNetworkingUtils.InitRelayNetworkAccess();

        NetSession.Instance.Lobby.TryParseLobbyCommand();
    }

    private void OnApplicationQuit()
    {
        NetSession.Instance.RemoveComponent();
        StringAllocator.UnpinSharedStringHandles();
    }

    [CwlPostLoad]
    private static void ClearRef()
    {
        CardGenEvent.HeldRefCards.Clear();
        SpatialGenEvent.HeldRefZones.Clear();
    }
}