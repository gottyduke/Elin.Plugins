using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using Cwl.Helper.Exceptions;
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
    internal const string Version = "0.8.9";

    [field: AllowNull]
    public static string BuildVersion => field ??= EmpMod.Assembly.GetName().Version.ToString();
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed class EmpMod : BaseUnityPlugin
{
    internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    internal static EmpMod Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

        EmpPop.InitLogger();
        EmpConfig.Bind();

        CommandRegistry.assemblies.Add(Assembly);
        Harmony.CreateAndPatchAll(Assembly, ModInfo.Guid);
    }

    private void Start()
    {
#if !DEBUG
        MonoFrame.AddVendorExclusion("MessagePack.");
#endif

        ResourceFetch.InvalidateTemp();

        SteamNetworkingUtils.InitRelayNetworkAccess();
    }

    private void OnApplicationQuit()
    {
        NetSession.Instance.RemoveComponent();
        StringAllocator.UnpinSharedStringHandles();
    }
}