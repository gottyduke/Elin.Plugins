using BepInEx.Configuration;

namespace Emmersive;

internal class EmConfig
{
    internal static void Bind(ConfigFile config)
    {
        Policy.Verbose = config.Bind(
            "RuntimePolicy",
            "Verbose",
            false,
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Debug用的信息输出\n" +
            "デバッグ用の詳細情報を出力(大量ログ発生の可能性あり)");

#if DEBUG
        Policy.Verbose.Value = true;
#endif

        Policy.Timeout = config.Bind(
            "RuntimePolicy",
            "Timeout",
            5f,
            new ConfigDescription(
                "Timeout in seconds for a generation request\n" +
                "When timeout, there'll be no retry attempt\n" +
                "一次生成请求的最大超时\n" +
                "超时后，将不会重新请求",
                new AcceptableValueRange<float>(1f, 20f)));

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

        Scene.NearbyRadius = config.Bind(
            "Scene",
            "NearbyRadius",
            -1,
            new ConfigDescription(
                "Radius in tiles to scan for nearby characters\n" +
                "Bigger radius may involve more characters and lengthy token input\n" +
                "Set to -1 to include all visible characters\n" +
                "以玩家为中心检测附近角色的半径块数\n" +
                "更大的半径会包含更多的角色，但会增加token消耗\n" +
                "设为-1会引用所有可见的角色",
                new AcceptableValueRange<int>(-1, 8)));

        Scene.TurnsCooldown = config.Bind(
            "Scene",
            "TurnsCooldown",
            8,
            new ConfigDescription(
                "Minimum turns required before next scene request\n" +
                "两次请求之间的最低间隔回合数",
                new AcceptableValueRange<int>(0, int.MaxValue)));

        Scene.SecondsCooldown = config.Bind(
            "Scene",
            "SecondsCooldown",
            6f,
            new ConfigDescription(
                "Minimum seconds in realtime required before next scene request\n" +
                "两次请求之间的最低间隔秒数",
                new AcceptableValueList<float>(0f, float.MaxValue)));

        Scene.BlockGlobalTalk = config.Bind(
            "Scene",
            "BlockGlobalTalk",
            true,
            "Block global character's talk and use them as context for scene play\n" +
            "May skip original talks if character is on scene play cooldown or API is unavailable\n" +
            "禁用全局角色的原版喊叫，并将它们用于上下文\n" +
            "如果该角色在冷却中或请求失败，可能会跳过该喊叫");
    }

    internal static class Scene
    {
        internal static ConfigEntry<int> MaxReactions { get; set; } = null!;
        internal static ConfigEntry<int> NearbyRadius { get; set; } = null!;
        internal static ConfigEntry<int> TurnsCooldown { get; set; } = null!;
        internal static ConfigEntry<float> SecondsCooldown { get; set; } = null!;
        internal static ConfigEntry<bool> BlockGlobalTalk { get; set; } = null!;
    }

    internal static class Context
    {
        internal static ConfigEntry<string> DisabledProviders { get; set; } = null!;
        internal static ConfigEntry<int> RecentLogDepth { get; set; } = null!;
    }

    internal static class Policy
    {
        internal static ConfigEntry<float> Timeout { get; set; } = null!;
        internal static ConfigEntry<bool> Verbose { get; set; } = null!;
    }
}