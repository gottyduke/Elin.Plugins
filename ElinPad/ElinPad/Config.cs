using BepInEx.Configuration;
using ReflexCLI.Attributes;

namespace ElinPad;

[ConsoleCommandClassCustomizer("ep.config")]
internal class PadConfig
{
    [ConsoleCommand] public static bool LoggingVerbose => Logging.Verbose?.Value is true;

    internal static void Load(ConfigFile config)
    {
        Logging.Verbose = config.Bind(
            ModInfo.Name,
            "Logging.Verbose",
            false,
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Debug用的信息输出");
    }

    internal class Logging
    {
        internal static ConfigEntry<bool>? Verbose { get; set; }
        internal static ConfigEntry<bool>? Execution { get; set; }
    }
}