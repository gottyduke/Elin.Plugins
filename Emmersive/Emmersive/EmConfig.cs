using BepInEx.Configuration;
using Emmersive.Components;
using ReflexCLI.Attributes;

namespace Emmersive;

[ConsoleCommandClassCustomizer("em")]
internal partial class EmConfig
{
    internal static void Bind()
    {
        var config = EmMod.Instance.Config;

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
            "Beta测试版默认启用\n" +
            "デバッグ用の詳細情報を出力(大量ログ発生の可能性あり)");


        Policy.Timeout = config.Bind(
            "RuntimePolicy",
            "Timeout",
            15f,
            new ConfigDescription(
                "Timeout in seconds for a generation request\n" +
                "Retry attempts will not be made after timeout\n" +
                "一次生成请求的最大超时\n" +
                "超时后，将不会重新请求",
                new AcceptableValueRange<float>(1f, 60f)));

        Policy.Retries = config.Bind(
            "RuntimePolicy",
            "Retries",
            1,
            new ConfigDescription(
                "Retries attempts after a failed request\n" +
                "生成请求失败后的重试次数",
                new AcceptableValueRange<int>(0, 5)));

        Policy.ConcurrentRequests = config.Bind(
            "RuntimePolicy",
            "ConcurrentRequests",
            1,
            new ConfigDescription(
                "Enable concurrent requests, which will make more scene reactions\n" +
                "Note that this will increase the token usage\n" +
                "启用并发请求，这将产生更多的场景反应\n" +
                "请注意，这会使你的令牌消耗增加",
                new AcceptableValueRange<int>(1, 5)));

        Policy.ServiceCooldown = config.Bind(
            "RuntimePolicy",
            "ServiceCooldown",
            15f,
            new ConfigDescription(
                "Minimum seconds in realtime required to auto reset an unavailable service\n" +
                "This is used for API service pooling\n" +
                "生成请求失败后的服务自动禁用时间\n" +
                "用于服务池自动管理下一可用服务",
                new AcceptableValueRange<float>(0f, 60f)));

        Context.DisabledProviders = config.Bind(
            "Context",
            "DisabledProviders",
            "",
            "Comma separated list of disabled context provider types\n" +
            "禁用的上下文提供器类型列表，以逗号分隔");

        Context.RecentLogDepth = config.Bind(
            "Context",
            "RecentLogDepth",
            20,
            new ConfigDescription(
                "Maximum number of previous logs to fetch as context\n" +
                "最大拉取的作为上下文的游戏日志条数",
                new AcceptableValueRange<int>(0, 100)));

        Context.RecentTalkOnly = config.Bind(
            "Context",
            "RecentTalkOnly",
            false,
            "Only fetch talk logs, do not include combat or gameplay info\n" +
            "仅拉取对话日志，不使用战斗或其他游戏信息");

        Context.NearbyRadius = config.Bind(
            "Context",
            "NearbyRadius",
            4,
            new ConfigDescription(
                "Radius in tiles to scan for nearby characters\n" +
                "Bigger radius may involve more characters and lengthy token input\n" +
                "以玩家为中心检测附近角色的半径块数\n" +
                "更大的半径会包含更多的角色，但会增加token消耗",
                new AcceptableValueRange<int>(0, 12)));

        Context.EnableLocalizer = config.Bind(
            "Context",
            "EnableLocalizer",
            false,
            "Localize the context entries, may increase prompt length\n" +
            "尝试本地化上下文条目，可能会增加提示词长度");

        Scene.MaxReactions = config.Bind(
            "Scene",
            "MaxReactions",
            4,
            new ConfigDescription(
                "Maximum reactions allowed in one single scene play request\n" +
                "More reactions may involve more characters\n" +
                "But also means lengthy and potentially out of context scene play\n" +
                "单次场景扮演请求允许的最大反应次数\n" +
                "反应次数多，可能涉及更多角色和互动\n" +
                "但场景可能耗时较长或偏离主题",
                new AcceptableValueRange<int>(1, 8)));

        Scene.TurnsCooldown = config.Bind(
            "Scene",
            "TurnsCooldown",
            12,
            "Minimum in game turns required before next scene request\n" +
            "Each character is tracked individually\n" +
            "两次请求之间的最低间隔回合数\n" +
            "每个角色单独计算");

        Scene.SecondsCooldown = config.Bind(
            "Scene",
            "SecondsCooldown",
            5f,
            new ConfigDescription(
                "Minimum seconds in realtime required before next scene request\n" +
                "Each character is tracked individually\n" +
                "两次请求之间的最低间隔秒数\n" +
                "每个角色单独计算",
                new AcceptableValueRange<float>(0f, 60f)));

        Scene.TurnsIdleTrigger = config.Bind(
            "Scene",
            "TurnsIdleTrigger",
            12,
            "Minimum in game turns required to auto trigger scene request\n" +
            "This is reset whenever a scene triggers\n" +
            "Set to -1 to disable this auto trigger\n" +
            "自动生成场景请求的回合数\n" +
            "生成任意请求时，该值会重置\n" +
            "设为-1禁用此功能");

        Scene.SceneBufferWindow = config.Bind(
            "Scene",
            "SceneBufferWindow",
            0.1f,
            new ConfigDescription(
                "Small window to buffer all scene trigger talks\n" +
                "This can prevent everyone talk at once such as after loading a save\n" +
                "Probably do not change this unless you are instructed to\n" +
                "抓取场景激活对话的缓冲时间\n" +
                "可以防止例如刚加载存档时所有人都同时说话的状况\n" +
                "除非知道在做什么，不建议更改",
                new AcceptableValueRange<float>(0f, 1f)));

        Scene.SceneBufferMode = config.Bind(
            "Scene",
            "SceneBufferMode",
            EmScheduler.ScheduleBufferMode.Incremental,
            "Buffering mode when using Scheduler.Buffer\n" +
            "Incremental: adds to the buffer\n" +
            "UniqueFrame: adds to the buffer, only once per frame\n" +
            "抓取场景激活对话的缓冲模式\n" +
            "Incremental: 连续增加缓冲时间\n" +
            "UniqueFrame: 每帧仅增加一次缓冲时间");

        Scene.BlockCharaTalk = config.Bind(
            "Scene",
            "BlockCharaTalk",
            true,
            "Block character talks when they are scheduled for a scene request\n" +
            "If timeout is set too long, the original barks may be skipped\n" +
            "如果角色已经计划于一次场景生成请求，则禁用该角色的原版气泡\n" +
            "如果生成请求超时设置的很长，可能会跳过该气泡");
    }

    internal static class Scene
    {
        internal static ConfigEntry<int> MaxReactions { get; set; } = null!;
        internal static ConfigEntry<int> TurnsIdleTrigger { get; set; } = null!;
        internal static ConfigEntry<float> SceneBufferWindow { get; set; } = null!;
        internal static ConfigEntry<EmScheduler.ScheduleBufferMode> SceneBufferMode { get; set; } = null!;
        internal static ConfigEntry<bool> BlockCharaTalk { get; set; } = null!;
        internal static ConfigEntry<int> TurnsCooldown { get; set; } = null!;
        internal static ConfigEntry<float> SecondsCooldown { get; set; } = null!;
    }

    internal static class Context
    {
        internal static ConfigEntry<string> DisabledProviders { get; set; } = null!;
        internal static ConfigEntry<int> RecentLogDepth { get; set; } = null!;
        internal static ConfigEntry<bool> RecentTalkOnly { get; set; } = null!;
        internal static ConfigEntry<int> NearbyRadius { get; set; } = null!;
        internal static ConfigEntry<bool> EnableLocalizer { get; set; } = null!;
    }

    internal static class Policy
    {
        internal static ConfigEntry<float> Timeout { get; set; } = null!;
        internal static ConfigEntry<int> Retries { get; set; } = null!;
        internal static ConfigEntry<int> ConcurrentRequests { get; set; } = null!;
        internal static ConfigEntry<bool> Verbose { get; set; } = null!;
        internal static ConfigEntry<float> ServiceCooldown { get; set; } = null!;
    }
}