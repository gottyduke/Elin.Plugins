using System.Reflection;
using BepInEx;
using EModding.Helper.Runtime;
using ReflexCLI;

namespace EModding;

public static class ModInfo
{
    public const string Guid = "elin.plugins.scripting";
    public const string Name = "Elin Scripting Kit";
    public const string Version = "1.1.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal partial class EModdingKit : BaseUnityPlugin
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    internal static EModdingKit? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        CommandRegistry.assemblies.Add(_assembly);
        ClassCache.typeLoaders.Add(TypeQualifier.TryQualify);
    }
}