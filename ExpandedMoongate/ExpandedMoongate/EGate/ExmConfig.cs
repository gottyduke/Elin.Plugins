using BepInEx.Configuration;
using ReflexCLI.Attributes;

namespace Exm;

[ConsoleCommandClassCustomizer("exm")]
internal partial class ExmConfig
{
    internal static void Bind()
    {
        var config = ExmMod.Instance.Config;

        Policy.Verbose = config.Bind(
            "RuntimePolicy",
            "Verbose",
#if DEBUG || true
            // enabled for beta builds
            true,
#else
            false,
#endif
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Enabled for beta testing by default\n" +
            "Debug用的信息输出\n" +
            "Beta测试版默认启用");


        Policy.Timeout = config.Bind(
            "RuntimePolicy",
            "Timeout",
            15f,
            new ConfigDescription(
                "Timeout in seconds for a query request\n" +
                "一次请求的最大超时",
                new AcceptableValueRange<float>(1f, 60f)));

        Display.MapsPerPage = config.Bind(
            "Display",
            "MapsPerPage",
            25,
            new ConfigDescription(
                "Maximum number of maps to display per page\n" +
                "一次显示的最大地图数量",
                new AcceptableValueRange<int>(1, 100)));

        Reload();
    }

    internal static class Policy
    {
        internal static ConfigEntry<bool> Verbose { get; set; } = null!;
        internal static ConfigEntry<float> Timeout { get; set; } = null!;
    }

    internal static class Display
    {
        internal static ConfigEntry<int> MapsPerPage { get; set; } = null!;
    }
}