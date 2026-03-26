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
    }

    internal static class Policy
    {
        internal static ConfigEntry<bool> Verbose { get; set; } = null!;
    }
}