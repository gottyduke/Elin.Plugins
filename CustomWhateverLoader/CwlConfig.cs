using BepInEx.Configuration;

namespace Cwl;

internal static class CwlConfig
{
    internal static ConfigEntry<bool>? VerboseLogging { get; set; }

    internal static class Source
    {
        internal static ConfigEntry<bool>? RethrowException { get; set; }
        internal static ConfigEntry<bool>? RelaxedImport { get; set; }
        internal static ConfigEntry<bool>? SheetMigrate { get; set; }
    }

    internal static void Load(ConfigFile config)
    {
        VerboseLogging = config.Bind(
            ModInfo.Name,
            "Logging.Verbose",
            false,
            "Verbose information that may be helpful for debugging");

        Source.RethrowException = config.Bind(
            ModInfo.Name,
            "Source.RethrowException",
            true,
            "Rethrow the excel exception as SourceParseException with more details attached");
        
        Source.RelaxedImport = config.Bind(
            ModInfo.Name,
            "Source.RelaxedImport",
            true,
            "When importing incompatible source sheets, let CWL try importing via column name instead of order");
        
        Source.SheetMigrate = config.Bind(
            ModInfo.Name,
            "Source.SheetMigrate",
            true,
            "(Experimental)\nWhen importing incompatible source sheets, generate migrated file in the same directory");
    }
}