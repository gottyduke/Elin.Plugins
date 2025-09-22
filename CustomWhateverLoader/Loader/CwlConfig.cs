using BepInEx.Configuration;
using ReflexCLI.Attributes;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace Cwl;

[ConsoleCommandClassCustomizer("cwl.config")]
public abstract class CwlConfig
{
    [ConsoleCommand] public static bool LoggingVerbose => Logging.Verbose!.Value;

    [ConsoleCommand] public static bool SeamlessStreaming => BGM.SeamlessStreaming!.Value;

    [ConsoleCommand] public static bool CacheTalks => Caching.Talks!.Value;
    [ConsoleCommand] public static bool CacheTypes => Caching.Types!.Value;
    [ConsoleCommand] public static bool CachePaths => Caching.Paths!.Value;
    [ConsoleCommand] public static bool CacheSourceSheets => Caching.SourceSheets!.Value;
    [ConsoleCommand] public static int CacheSourceSheetsRetention => Caching.SourceSheetsRetention!.Value;
    [ConsoleCommand] public static bool CacheSprites => Caching.Sprites!.Value;

    [ConsoleCommand] public static bool ExpandedActions => Dialog.ExpandedActions!.Value;
    [ConsoleCommand] public static bool ExpandedActionsExternal => Dialog.ExpandedActionsAllowExternal!.Value;
    [ConsoleCommand] public static bool NoOverlappingSounds => Dialog.NoOverlappingSounds!.Value;
    [ConsoleCommand] public static bool VariableQuote => Dialog.VariableQuote!.Value;

    [ConsoleCommand] public static bool ExceptionAnalyze => Exceptions.Analyze!.Value;
    [ConsoleCommand] public static bool ExceptionPopup => Exceptions.Popup!.Value;

    [ConsoleCommand] public static bool FixBaseGameAvatar => Patches.FixBaseGameAvatar!.Value;
    [ConsoleCommand] public static bool FixBaseGamePopup => Patches.FixBaseGamePopup!.Value;
    [ConsoleCommand] public static bool QualifyTypeName => Patches.QualifyTypeName!.Value;
    [ConsoleCommand] public static bool SafeCreateClass => Patches.SafeCreateClass!.Value;

    [ConsoleCommand] public static bool AllowProcessors => Source.AllowProcessors!.Value;
    [ConsoleCommand] public static int MaxPrefetchLoads => Source.MaxPrefetchLoads!.Value;
    [ConsoleCommand] public static bool NamedImport => Source.NamedImport!.Value;
    [ConsoleCommand] public static bool OverrideSameId => Source.OverrideSameId!.Value;
    [ConsoleCommand] public static bool RethrowException => Source.RethrowException!.Value;

    [ConsoleCommand] public static bool SheetInspection => Source.SheetInspection!.Value;

    // TODO: disabled due to frequent game updates
    [ConsoleCommand] public static bool SheetMigrate => Source.SheetMigrate!.Value && false;
    [ConsoleCommand] public static bool TrimSpaces => Source.TrimSpaces!.Value;

    internal static void Load(ConfigFile config)
    {
        Logging.Verbose = config.Bind(
            ModInfo.Name,
            "Logging.Verbose",
            false,
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Debug用的信息输出\n" +
            "デバッグ用の詳細情報を出力(大量ログ発生の可能性あり)");

        Logging.Execution = config.Bind(
            ModInfo.Name,
            "Logging.Execution",
            true,
            "Measure the extra loading time added by CWL, this is displayed in Player.log\n" +
            "记录CWL运行时间\n" +
            "CWLの実行時間を記録");

        Exceptions.Analyze = config.Bind(
            ModInfo.Name,
            "Exceptions.Analyze",
            true,
            "Analyze the unhandled exception during gameplay and log the results\n" +
            "分析游戏运行时抛出的异常\n" +
            "ゲーム実行時に発生した例外を分析記録");

        Exceptions.Popup = config.Bind(
            ModInfo.Name,
            "Exceptions.Popup",
            true,
            "Display a popup for the analyzed unhandled exception\n" +
            "在游戏中显示运行时抛出的异常\n" +
            "ゲーム画面に解析済み例外のポップアップを表示");

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
            "当切换播放列表时，如果当前播放的曲目在新播放列表中，则尝试无缝衔接\n" +
            "再生リスト切り替え時に、現在再生中の曲が新しいリストに含まれる場合、シームレスな継続再生を試行");

        Caching.Paths = config.Bind(
            ModInfo.Name,
            "Caching.Paths",
            true,
            "Cache paths relocated by CWL instead of iterating new paths\n" +
            "缓存CWL重定向的路径而不是每次重新搜索\n" +
            "CWL リダイレクトのパスを毎回再検索するのではなくキャッシュします");

        Caching.SourceSheets = config.Bind(
            ModInfo.Name,
            "Caching.SourceSheets",
            true,
            "Cache source sheets imported by CWL in persistent data storage\n" +
            "缓存CWL导入的源表而不是每次重新导入\n" +
            "毎回再インポートするのではなく、CWL インポートのソース テーブルをキャッシュします");

        Caching.SourceSheetsRetention = config.Bind(
            ModInfo.Name,
            "Caching.SourceSheetsRetention",
            30,
            new ConfigDescription("Retention period in days before automatically regenerating source sheets cache\n" +
                                  "源表缓存有效期(日)\n" +
                                  "ソーステーブルキャッシュの有効期間（日数）",
                new AcceptableValueRange<int>(0, 30)));

        Caching.Sprites = config.Bind(
            ModInfo.Name,
            "Caching.Sprites",
            true,
            "Cache sprites created by CWL instead of creating new from textures\n" +
            "缓存CWL生成的贴图而不是每次重新构建\n" +
            "CWLで生成した画像を毎回再構築せずキャッシュ");

        Caching.Talks = config.Bind(
            ModInfo.Name,
            "Caching.Talks",
            true,
            "Cache GetDialog/HasTopic talks instead of loading & building map everytime\n" +
            "缓存GetDialog/HasTopic构建的对话文本表而不是每次都重新加载+构建\n" +
            "GetDialog/HasTopicを使用して構築された対話テキスト表を毎回再読み込みして構築するのではなく、キャッシュを利用する");

        Caching.Types = config.Bind(
            ModInfo.Name,
            "Caching.Types",
            true,
            "Cache ClassCache types early instead of roundtrip lookup & querying all assemblies\n" +
            "提前ClassCache的类缓存优先级而不是每次重新搜索\n" +
            "クラスキャッシュ（ClassCache）のクラスキャッシュの優先度を前もって設定し、毎回再検索するのではなくします");

        Dialog.DynamicCheckIf = config.Bind(
            ModInfo.Name,
            "Dialog.DynamicCheckIf",
            true,
            "Dynamically re-evaluate if conditions during drama play instead of only once on load\n" +
            "剧情演出时动态判断if条件而非仅在加载时判断一次");

        Dialog.ExpandedActions = config.Bind(
            ModInfo.Name,
            "Dialog.ExpandedActions",
            true,
            "Expand the actions table for drama sheets for mod authors to utilize\n" +
            "为剧情表启用action拓展，Mod作者能够利用更多功能设计剧情表\n" +
            "台本シートのアクションテーブルを拡張し、MOD作者が追加機能を利用可能に");

        Dialog.ExpandedActionsAllowExternal = config.Bind(
            ModInfo.Name,
            "Dialog.ExpandedActionsAllowExternal",
            true,
            "Allow invoking external methods from other assemblies within the drama sheet, this may be unstable\n" +
            "为剧情表启用action拓展时同时允许调用外部程序集的方法，这可能不稳定\n" +
            "アクション拡張時に外部アセンブリのメソッド呼び出しを許可(不安定性あり)");

        Dialog.NoOverlappingSounds = config.Bind(
            ModInfo.Name,
            "Dialog.NoOverlappingSounds",
            true,
            "During dialogs, prevent sound actions from overlapping with each other by stopping previous sound first\n" +
            "对话中的sound动作不会彼此重叠 - 上一个音源会先被停止\n" +
            "会話中のsoundアクションが重複しないように、前回音源を停止してから再生");

        Dialog.VariableQuote = config.Bind(
            ModInfo.Name,
            "Dialog.VariableQuote",
            true,
            "For talk texts, allow both JP quote 「」 and EN quote \"\" to be used as Msg.colors.Talk identifier\n" +
            "对话文本允许日本引号和英语引号同时作为Talk颜色检测词\n" +
            "台本テキストで日本語括弧「」と英語引用符\"\"、および現在言語の引用符を`Msg.colors.Talk`識別子として共用可能に");

        Patches.FixBaseGameAvatar = config.Bind(
            ModInfo.Name,
            "Patches.FixBaseGameAvatar",
            true,
            "When repositioning custom character icons, let CWL fix base game characters too\n" +
            "E.g. fairy icons are usually clipping through upper border\n" +
            "在重新调整自定义角色头像位置时，让CWL也修复游戏本体角色头像位置。例如，妖精角色的头像通常会超出边界\n" +
            "カスタムキャラクターアイコン位置調整時、本体キャラクターの位置も修正(例：妖精アイコンが境界を超える問題)");

        Patches.FixBaseGamePopup = config.Bind(
            ModInfo.Name,
            "Patches.FixBaseGamePopup",
            true,
            "When repositioning custom character pop ups, let CWL fix base game characters too\n" +
            "E.g. using custom skins will result the speech bubble and emote icons shown way above their heads\n" +
            "在重新调整自定义角色气泡位置时，让CWL也修复游戏本体角色气泡位置。例如，更改贴图皮肤的角色的气泡框会显示的很高\n" +
            "カスタムキャラクター吹き出し位置調整時、本体キャラクターの位置も修正(例：スキン変更時の吹き出し位置ズレ)");

        Patches.QualifyTypeName = config.Bind(
            ModInfo.Name,
            "Patches.QualifyTypeName",
            true,
            "When importing custom classes for class cache, let CWL qualify its type name\n" +
            "Element, BaseCondition, Trait, Zone\n" +
            "当为类缓存导入自定义类时，让CWL为其生成限定类型名\n" +
            "クラスキャッシュへのカスタムクラスインポート時、CWLに完全修飾型名を生成させる");

        Patches.SafeCreateClass = config.Bind(
            ModInfo.Name,
            "Patches.SafeCreateClass",
            true,
            "When custom classes fail to create from save, let CWL replace it with a safety cone and keep the game going\n" +
            "当自定义类无法加载时，让CWL将其替换为安全锥以保持游戏进行\n" +
            "カスタムクラスのロード失敗時、安全コーンで代替してゲームを継続");

        Source.AllowProcessors = config.Bind(
            ModInfo.Name,
            "Source.AllowProcessors",
            true,
            "Allow CWL to run pre/post processors for workbook, sheet, and cells\n" +
            "允许CWL为工作簿、工作表、单元格执行预/后处理\n" +
            "ワークブック・ワークシート・セルに対する前処理/後処理の実行を許可");

        Source.MaxPrefetchLoads = config.Bind(
            ModInfo.Name,
            "Source.MaxPrefetchLoads",
            64,
            "Allow CWL to prefetch sheets for way faster loading, use -1 to disable\n" +
            "允许CWL预加载部分源表以极大减少加载时间，设为 -1 禁用");

        Source.NamedImport = config.Bind(
            ModInfo.Name,
            "Source.NamedImport",
            true,
            "When importing incompatible source sheets, try importing via column name instead of order\n" +
            "当导入可能不兼容的源表时，允许CWL使用列名代替列序导入\n" +
            "互換性のないソースシートインポート時、列番号ではなく列名を使用したインポートを許可");

        Source.OverrideSameId = config.Bind(
            ModInfo.Name,
            "Source.OverrideSameId",
            true,
            "When importing rows with an existing ID, replace it instead of adding duplicate rows\n" +
            "当导入重复ID的行时，覆盖它而不是添加新同ID行\n" +
            "重複IDの行をインポート時、新規追加せず既存行を上書き");

        Source.RethrowException = config.Bind(
            ModInfo.Name,
            "Source.RethrowException",
            true,
            "Rethrow the excel exception as SourceParseException with more details attached\n" +
            "当捕获Excel解析异常时，生成当前单元格详细信息并重抛为SourceParseException\n" +
            "Excel解析例外発生時、セル詳細情報を付加したSourceParseExceptionとして再送出");

        Source.SheetInspection = config.Bind(
            ModInfo.Name,
            "Source.SheetInspection",
            true,
            "When importing incompatible source sheets, dump headers for debugging purposes\n" +
            "当导入可能不兼容的源表时，吐出该表的详细信息\n" +
            "互換性のないソースシートインポート時、デバッグ用に詳細情報を出力");

        Source.SheetMigrate = config.Bind(
            ModInfo.Name,
            "Source.SheetMigrate",
            false,
            "(Experimental)\nWhen importing incompatible source sheets, generate migrated file in the same directory\n" +
            "(实验性) 当导入可能不兼容的源表时，在同一目录生成当前版本的升级表\n" +
            "互換性のないソースシートインポート時、同ディレクトリに移行済みファイルを生成");

        Source.TrimSpaces = config.Bind(
            ModInfo.Name,
            "Source.TrimSpaces",
            true,
            "Trim all leading and trailing spaces from cell value\nRequires Source.AllowProcessors to be true\n" +
            "移除单元格数据的前后空格文本，需要允许执行单元格后处理\n" +
            "セルデータの前後空白文字を削除(セル後処理の実行許可が必要)");
    }

    internal abstract class Logging
    {
        internal static ConfigEntry<bool>? Verbose { get; set; }
        internal static ConfigEntry<bool>? Execution { get; set; }
    }

    internal abstract class BGM
    {
        internal static ConfigEntry<bool>? SeamlessStreaming { get; set; }
    }

    internal abstract class Caching
    {
        internal static ConfigEntry<bool>? Talks { get; set; }
        internal static ConfigEntry<bool>? Types { get; set; }
        internal static ConfigEntry<bool>? Paths { get; set; }
        internal static ConfigEntry<bool>? SourceSheets { get; set; }
        internal static ConfigEntry<int>? SourceSheetsRetention { get; set; }
        internal static ConfigEntry<bool>? Sprites { get; set; }
    }

    internal abstract class Dialog
    {
        internal static ConfigEntry<bool>? DynamicCheckIf { get; set; }
        internal static ConfigEntry<bool>? ExpandedActions { get; set; }
        internal static ConfigEntry<bool>? ExpandedActionsAllowExternal { get; set; }
        internal static ConfigEntry<bool>? NoOverlappingSounds { get; set; }
        internal static ConfigEntry<bool>? VariableQuote { get; set; }
    }

    internal abstract class Exceptions
    {
        internal static ConfigEntry<bool>? Analyze { get; set; }
        internal static ConfigEntry<bool>? Popup { get; set; }
    }

    internal abstract class Patches
    {
        internal static ConfigEntry<bool>? FixBaseGameAvatar { get; set; }
        internal static ConfigEntry<bool>? FixBaseGamePopup { get; set; }
        internal static ConfigEntry<bool>? QualifyTypeName { get; set; }
        internal static ConfigEntry<bool>? SafeCreateClass { get; set; }
    }

    internal abstract class Source
    {
        internal static ConfigEntry<bool>? AllowProcessors { get; set; }
        internal static ConfigEntry<int>? MaxPrefetchLoads { get; set; }
        internal static ConfigEntry<bool>? NamedImport { get; set; }
        internal static ConfigEntry<bool>? OverrideSameId { get; set; }
        internal static ConfigEntry<bool>? RethrowException { get; set; }
        internal static ConfigEntry<bool>? SheetInspection { get; set; }
        internal static ConfigEntry<bool>? SheetMigrate { get; set; }
        internal static ConfigEntry<bool>? TrimSpaces { get; set; }
    }
}