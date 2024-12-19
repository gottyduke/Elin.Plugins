using BepInEx.Configuration;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace Cwl.Loader;

public class CwlConfig
{
    public static bool CachePaths => Caching.Paths?.Value is true;
    public static bool CacheSprites => Caching.Sprites?.Value is true;

    public static bool QualifyTypeName => Patches.QualifyTypeName?.Value is true;
    public static bool SafeCreateClass => Patches.SafeCreateClass?.Value is true;
    public static bool VariableQuote => Dialog.VariableQuote?.Value is true;
    public static bool AllowProcessors => Source.AllowProcessors?.Value is true;
    public static bool NamedImport => Source.NamedImport?.Value is true;
    public static bool RethrowException => Source.RethrowException?.Value is true;
    public static bool SheetInspection => Source.SheetInspection?.Value is true;

    // TODO: disabled due to frequent game updates
    public static bool SheetMigrate => false;
    public static bool TrimSpaces => Source.TrimSpaces?.Value is true;

    internal static void Load(ConfigFile config)
    {
        Logging.Verbose = config.Bind(
            ModInfo.Name,
            "Logging.Verbose",
            false,
            "Verbose information that may be helpful(spamming) for debugging");

        Logging.Execution = config.Bind(
            ModInfo.Name,
            "Logging.Execution",
            true,
            "Measure the extra loading time added by CWL, this is displayed in Player.log");

        Caching.Paths = config.Bind(
            ModInfo.Name,
            "Caching.Paths",
            true,
            "Cache paths relocated by CWL instead of iterating new paths");

        Caching.Sprites = config.Bind(
            ModInfo.Name,
            "Caching.Sprites",
            true,
            "Cache sprites created by CWL instead of creating new from textures");

        Dialog.VariableQuote = config.Bind(
            ModInfo.Name,
            "Dialog.VariableQuote",
            true,
            "For talk texts, allow both JP quote 「」 and EN quote \"\" to be used as Msg.colors.Talk");

        Patches.QualifyTypeName = config.Bind(
            ModInfo.Name,
            "Patches.QualifyTypeName",
            true,
            "When importing custom classes for class cache, let CWL qualify its type name");

        Patches.SafeCreateClass = config.Bind(
            ModInfo.Name,
            "Patches.SafeCreateClass",
            true,
            "When custom classes fail to create from save, let CWL replace it with a safety cone and keep the game going");

        Source.AllowProcessors = config.Bind(
            ModInfo.Name,
            "Source.AllowProcessors",
            true,
            "Allow CWL to run pre/post processors for workbook, sheet, and cells.");

        Source.RethrowException = config.Bind(
            ModInfo.Name,
            "Source.RethrowException",
            true,
            "Rethrow the excel exception as SourceParseException with more details attached");

        Source.TrimSpaces = config.Bind(
            ModInfo.Name,
            "Source.TrimSpaces",
            true,
            "Trim all leading and trailing spaces from cell value\nRequires Source.AllowProcessors to be true");

        Source.NamedImport = config.Bind(
            ModInfo.Name,
            "Source.NamedImport",
            true,
            "When importing incompatible source sheets, try importing via column name instead of order");

        Source.SheetInspection = config.Bind(
            ModInfo.Name,
            "Source.SheetInspection",
            true,
            "When importing incompatible source sheets, dump headers for debugging purposes");

        Source.SheetMigrate = config.Bind(
            ModInfo.Name,
            "Source.SheetMigrate",
            false,
            "(Experimental)\nWhen importing incompatible source sheets, generate migrated file in the same directory");
    }

    internal class Logging
    {
        internal static ConfigEntry<bool>? Verbose { get; set; }
        internal static ConfigEntry<bool>? Execution { get; set; }
    }

    internal class Patches
    {
        internal static ConfigEntry<bool>? QualifyTypeName { get; set; }
        internal static ConfigEntry<bool>? SafeCreateClass { get; set; }
    }

    internal class Caching
    {
        internal static ConfigEntry<bool>? Paths { get; set; }
        internal static ConfigEntry<bool>? Sprites { get; set; }
    }

    internal class Dialog
    {
        internal static ConfigEntry<bool>? VariableQuote { get; set; }
    }

    internal class Source
    {
        internal static ConfigEntry<bool>? AllowProcessors { get; set; }
        internal static ConfigEntry<bool>? RethrowException { get; set; }
        internal static ConfigEntry<bool>? TrimSpaces { get; set; }
        internal static ConfigEntry<bool>? NamedImport { get; set; }
        internal static ConfigEntry<bool>? SheetInspection { get; set; }
        internal static ConfigEntry<bool>? SheetMigrate { get; set; }
    }
}