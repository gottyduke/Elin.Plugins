using BepInEx.Configuration;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace Cwl;

public class CwlConfig
{
    public static bool CachePaths => Caching.Paths?.Value is true;
    public static bool CacheSprites => Caching.Sprites?.Value is true;

    public static bool QualifyTypeName => Patches.QualifyTypeName?.Value is true;
    public static bool FixBaseGameAvatar => Patches.FixBaseGameAvatar?.Value is true;
    public static bool SafeCreateClass => Patches.SafeCreateClass?.Value is true;
    public static bool NoOverlappingSounds => Dialog.NoOverlappingSounds?.Value is true;
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
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Debug用的信息输出");

        Logging.Execution = config.Bind(
            ModInfo.Name,
            "Logging.Execution",
            true,
            "Measure the extra loading time added by CWL, this is displayed in Player.log\n" +
            "记录CWL运行时间");

        Caching.Paths = config.Bind(
            ModInfo.Name,
            "Caching.Paths",
            true,
            "Cache paths relocated by CWL instead of iterating new paths\n" +
            "缓存CWL重定向的路径而不是每次重新搜索");

        Caching.Sprites = config.Bind(
            ModInfo.Name,
            "Caching.Sprites",
            true,
            "Cache sprites created by CWL instead of creating new from textures\n" +
            "缓存CWL生成的贴图而不是每次重新构建");

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

    internal class Patches
    {
        internal static ConfigEntry<bool>? QualifyTypeName { get; set; }
        internal static ConfigEntry<bool>? FixBaseGameAvatar { get; set; }
        internal static ConfigEntry<bool>? SafeCreateClass { get; set; }
    }

    internal class Caching
    {
        internal static ConfigEntry<bool>? Paths { get; set; }
        internal static ConfigEntry<bool>? Sprites { get; set; }
    }

    internal class Dialog
    {
        internal static ConfigEntry<bool>? NoOverlappingSounds { get; set; }
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