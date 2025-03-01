using BepInEx.Configuration;
using ReflexCLI.Attributes;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl.config")]
public class CwlConfig
{
    [ConsoleCommand] public static bool LoggingVerbose => Logging.Verbose?.Value is true;

    [ConsoleCommand] public static bool SeamlessStreaming => BGM.SeamlessStreaming?.Value is true;

    [ConsoleCommand] public static bool CachePaths => Caching.Paths?.Value is true;
    [ConsoleCommand] public static bool CacheSheets => Caching.Sheets?.Value is true;
    [ConsoleCommand] public static bool CacheSprites => Caching.Sprites?.Value is true;

    [ConsoleCommand] public static bool ExpandedActions => Dialog.ExpandedActions?.Value is true;
    [ConsoleCommand] public static bool ExpandedActionsExternal => Dialog.ExpandedActionsAllowExternal?.Value is true;
    [ConsoleCommand] public static bool NoOverlappingSounds => Dialog.NoOverlappingSounds?.Value is true;
    [ConsoleCommand] public static bool VariableQuote => Dialog.VariableQuote?.Value is true;

    [ConsoleCommand] public static bool ExceptionAnalyze => Exceptions.Analyze?.Value is true;
    [ConsoleCommand] public static bool ExceptionPopup => Exceptions.Popup?.Value is true;

    [ConsoleCommand] public static bool QualifyTypeName => Patches.QualifyTypeName?.Value is true;
    [ConsoleCommand] public static bool FixBaseGameAvatar => Patches.FixBaseGameAvatar?.Value is true;
    [ConsoleCommand] public static bool SafeCreateClass => Patches.SafeCreateClass?.Value is true;

    [ConsoleCommand] public static bool AllowProcessors => Source.AllowProcessors?.Value is true;
    [ConsoleCommand] public static bool NamedImport => Source.NamedImport?.Value is true;
    [ConsoleCommand] public static bool OverrideSameId => Source.OverrideSameId?.Value is true;
    [ConsoleCommand] public static bool RethrowException => Source.RethrowException?.Value is true;

    [ConsoleCommand] public static bool SheetInspection => Source.SheetInspection?.Value is true;

    // TODO: disabled due to frequent game updates
    [ConsoleCommand] public static bool SheetMigrate => false;
    [ConsoleCommand] public static bool TrimSpaces => Source.TrimSpaces?.Value is true;

    internal static void Load(ConfigFile config)
    {
        Logging.Verbose = config.Bind(
            ModInfo.Name,
            "Logging.Verbose",
            false,
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Debug用的信息输出");

        Logging.Execution = config.Bind(
            ModInfo.Name,
            "Logging.Execution",
            true,
            "Measure the extra loading time added by CWL, this is displayed in Player.log\n" +
            "记录CWL运行时间");

        Exceptions.Analyze = config.Bind(
            ModInfo.Name,
            "Exceptions.Analyze",
            true,
            "Analyze the unhandled exception during gameplay and log the results\n" +
            "分析游戏运行时抛出的异常");

        Exceptions.Popup = config.Bind(
            ModInfo.Name,
            "Exceptions.Popup",
            true,
            "Display a popup for the analyzed unhandled exception\n" +
            "在游戏中显示运行时抛出的异常");

#if DEBUG
        Logging.Verbose.Value = true;
        Logging.Execution.Value = true;
        Exceptions.Analyze.Value = true;
        Exceptions.Popup.Value = true;
#endif

        BGM.SeamlessStreaming = config.Bind(
            ModInfo.Name,
            "BGM.SeamlessStreaming",
            true,
            "When switching to a new playlist, if current playing BGM is included in the new playlist, seamlessly stream it\n" +
            "当切换播放列表时，如果当前播放的曲目在新播放列表中，则尝试无缝衔接");

        Caching.Paths = config.Bind(
            ModInfo.Name,
            "Caching.Paths",
            true,
            "Cache paths relocated by CWL instead of iterating new paths\n" +
            "缓存CWL重定向的路径而不是每次重新搜索");

        Caching.Sheets = config.Bind(
            ModInfo.Name,
            "Caching.Sheets",
            true,
            "Cache source sheets loaded by CWL which will load much faster when it's unchanged after caching\n" +
            "缓存CWL加载的源表，缓存后的源表如果没有修改则能够以极高的速度加载");

        Caching.Sprites = config.Bind(
            ModInfo.Name,
            "Caching.Sprites",
            true,
            "Cache sprites created by CWL instead of creating new from textures\n" +
            "缓存CWL生成的贴图而不是每次重新构建");

        Dialog.ExpandedActions = config.Bind(
            ModInfo.Name,
            "Dialog.ExpandedActions",
            true,
            "Expand the actions table for drama sheets for mod authors to utilize\n" +
            "为剧情表启用action拓展，Mod作者能够利用更多功能设计剧情表");

        Dialog.ExpandedActionsAllowExternal = config.Bind(
            ModInfo.Name,
            "Dialog.ExpandedActionsAllowExternal",
            true,
            "Allow invoking external methods from other assemblies within the drama sheet, this may be unstable\n" +
            "为剧情表启用action拓展时同时允许调用外部程序集的方法，这可能不稳定");

        Dialog.NoOverlappingSounds = config.Bind(
            ModInfo.Name,
            "Dialog.NoOverlappingSounds",
            true,
            "During dialogs, prevent sound actions from overlapping with each other by stopping previous sound first\n" +
            "对话中的sound动作不会彼此重叠 - 上一个音源会先被停止");

        Dialog.VariableQuote = config.Bind(
            ModInfo.Name,
            "Dialog.VariableQuote",
            true,
            "For talk texts, allow both JP quote 「」 and EN quote \"\" to be used as Msg.colors.Talk identifier\n" +
            "对话文本允许日本引号和英语引号同时作为Talk颜色检测词");

        Patches.FixBaseGameAvatar = config.Bind(
            ModInfo.Name,
            "Patches.FixBaseGameAvatar",
            true,
            "When repositioning custom character icon positions, let CWL fix base game characters too\n" +
            "E.g. fairy icons are usually clipping through upper border\n" +
            "在重新定位自定义角色头像位置时，让CWL也修复游戏本体角色头像位置。例如，妖精角色的头像通常会超出边界");

        Patches.QualifyTypeName = config.Bind(
            ModInfo.Name,
            "Patches.QualifyTypeName",
            true,
            "When importing custom classes for class cache, let CWL qualify its type name\n" +
            "Element, BaseCondition, Trait, Zone\n" +
            "当为类缓存导入自定义类时，让CWL为其生成限定类型名");

        Patches.SafeCreateClass = config.Bind(
            ModInfo.Name,
            "Patches.SafeCreateClass",
            true,
            "When custom classes fail to create from save, let CWL replace it with a safety cone and keep the game going\n" +
            "当自定义类无法加载时，让CWL将其替换为安全锥以保持游戏进行");

        Source.AllowProcessors = config.Bind(
            ModInfo.Name,
            "Source.AllowProcessors",
            true,
            "Allow CWL to run pre/post processors for workbook, sheet, and cells\n" +
            "允许CWL为工作簿、工作表、单元格执行预/后处理");

        Source.NamedImport = config.Bind(
            ModInfo.Name,
            "Source.NamedImport",
            true,
            "When importing incompatible source sheets, try importing via column name instead of order\n" +
            "当导入可能不兼容的源表时，允许CWL使用列名代替列序导入");

        Source.OverrideSameId = config.Bind(
            ModInfo.Name,
            "Source.OverrideSameId",
            true,
            "When importing rows with an existing ID, replace it instead of adding duplicate rows\n" +
            "当导入重复ID的行时，覆盖它而不是添加新同ID行");

        Source.RethrowException = config.Bind(
            ModInfo.Name,
            "Source.RethrowException",
            true,
            "Rethrow the excel exception as SourceParseException with more details attached\n" +
            "当捕获Excel解析异常时，生成当前单元格详细信息并重抛为SourceParseException");

        Source.SheetInspection = config.Bind(
            ModInfo.Name,
            "Source.SheetInspection",
            true,
            "When importing incompatible source sheets, dump headers for debugging purposes\n" +
            "当导入可能不兼容的源表时，吐出该表的详细信息");

        Source.SheetMigrate = config.Bind(
            ModInfo.Name,
            "Source.SheetMigrate",
            false,
            "(Experimental)\nWhen importing incompatible source sheets, generate migrated file in the same directory\n" +
            "(实验性) 当导入可能不兼容的源表时，在同一目录生成当前版本的升级表");

        Source.TrimSpaces = config.Bind(
            ModInfo.Name,
            "Source.TrimSpaces",
            true,
            "Trim all leading and trailing spaces from cell value\nRequires Source.AllowProcessors to be true\n" +
            "移除单元格数据的前后空格文本，需要允许执行单元格后处理");
    }

    internal class Logging
    {
        internal static ConfigEntry<bool>? Verbose { get; set; }
        internal static ConfigEntry<bool>? Execution { get; set; }
    }

    internal class BGM
    {
        internal static ConfigEntry<bool>? SeamlessStreaming { get; set; }
    }

    internal class Caching
    {
        internal static ConfigEntry<bool>? Paths { get; set; }
        internal static ConfigEntry<bool>? Sheets { get; set; }
        internal static ConfigEntry<bool>? Sprites { get; set; }
    }

    internal class Dialog
    {
        internal static ConfigEntry<bool>? ExpandedActions { get; set; }
        internal static ConfigEntry<bool>? ExpandedActionsAllowExternal { get; set; }
        internal static ConfigEntry<bool>? NoOverlappingSounds { get; set; }
        internal static ConfigEntry<bool>? VariableQuote { get; set; }
    }

    internal class Exceptions
    {
        internal static ConfigEntry<bool>? Analyze { get; set; }
        internal static ConfigEntry<bool>? AutoFix { get; set; }
        internal static ConfigEntry<bool>? Popup { get; set; }
    }

    internal class Patches
    {
        internal static ConfigEntry<bool>? FixBaseGameAvatar { get; set; }
        internal static ConfigEntry<bool>? QualifyTypeName { get; set; }
        internal static ConfigEntry<bool>? SafeCreateClass { get; set; }
    }

    internal class Source
    {
        internal static ConfigEntry<bool>? AllowProcessors { get; set; }
        internal static ConfigEntry<bool>? NamedImport { get; set; }
        internal static ConfigEntry<bool>? OverrideSameId { get; set; }
        internal static ConfigEntry<bool>? RethrowException { get; set; }
        internal static ConfigEntry<bool>? SheetInspection { get; set; }
        internal static ConfigEntry<bool>? SheetMigrate { get; set; }
        internal static ConfigEntry<bool>? TrimSpaces { get; set; }
    }
}