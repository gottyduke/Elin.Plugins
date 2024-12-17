using BepInEx.Configuration;

namespace Cwl.Loader;

public class CwlConfig
{
    internal static void Load(ConfigFile config)
    {
        Logging.Verbose = config.Bind(
            ModInfo.Name,
            "Logging.Verbose",
            false,
            "Verbose information that may be helpful for debugging");

        Logging.Execution = config.Bind(
            ModInfo.Name,
            "Logging.Execution",
            true,
            "Measure the extra loading time added by CWL");

        Source.RethrowException = config.Bind(
            ModInfo.Name,
            "Source.RethrowException",
            true,
            "Rethrow the excel exception as SourceParseException with more details attached");

        Source.TrimSpaces = config.Bind(
            ModInfo.Name,
            "Source.TrimSpaces",
            true,
            "Trim all leading and trailing spaces from cell value");

        Source.NamedImport = config.Bind(
            ModInfo.Name,
            "Source.NamedImport",
            true,
            "(Experimental)\nWhen importing incompatible source sheets, try importing via column name instead of order");

        Source.SheetMigrate = config.Bind(
            ModInfo.Name,
            "Source.SheetMigrate",
            true,
            "(Experimental)\nWhen importing incompatible source sheets, generate migrated file in the same directory");
    }

    internal class Logging
    {
        internal static ConfigEntry<bool>? Verbose { get; set; }
        internal static ConfigEntry<bool>? Execution { get; set; }
    }

    public class Source
    {
        public static ConfigEntry<bool>? RethrowException { get; internal set; }
        public static ConfigEntry<bool>? TrimSpaces { get; internal set; }
        public static ConfigEntry<bool>? NamedImport { get; internal set; }
        public static ConfigEntry<bool>? SheetMigrate { get; internal set; }
    }
}