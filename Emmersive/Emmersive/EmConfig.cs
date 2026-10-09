using BepInEx.Configuration;
using ReflexCLI.Attributes;
using UnityEngine;

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
#if DEBUG
            true,
#else
            false,
#endif
            "Write detailed debug information to Player.log (lots of lines)\n" +
            "在 Player.log 里输出详细的调试信息（日志量很大）");

        Policy.Timeout = config.Bind(
            "RuntimePolicy",
            "Timeout",
            15f,
            new ConfigDescription(
                "Seconds to wait for an AI reply (1-60). Ollama always waits at least 60 seconds\n" +
                "A timed-out request is not retried, and the service cools down for ServiceCooldown seconds\n" +
                "等待 AI 回复的最长秒数（1–60），Ollama 至少等待 60 秒\n" +
                "超时后不会换服务重试，该服务会暂停使用 ServiceCooldown 秒",
                new AcceptableValueRange<float>(1f, 60f)));

        Policy.Retries = config.Bind(
            "RuntimePolicy",
            "Retries",
            1,
            new ConfigDescription(
                "After a failed request, how many more times to try with the next service in the list (0-5)\n" +
                "Never more than the number of services minus 1. Timeouts and HTTP 400/401/403/404 errors are not retried\n" +
                "请求失败后，最多再换列表中的下一个服务重试几次（0–5）\n" +
                "不会超过服务数减 1；超时以及 HTTP 400/401/403/404 错误不会重试",
                new AcceptableValueRange<int>(0, 5)));

        Policy.ConcurrentRequests = config.Bind(
            "RuntimePolicy",
            "ConcurrentRequests",
            1,
            new ConfigDescription(
                "Maximum number of AI requests running at the same time (1-5)\n" +
                "1 is enough for most players. Higher values use more tokens\n" +
                "最多同时进行几个 AI 请求（1–5）\n" +
                "一般保持 1 即可，调高会增加 Token 消耗",
                new AcceptableValueRange<int>(1, 5)));

        Policy.GlobalRequestCooldown = config.Bind(
            "RuntimePolicy",
            "GlobalRequestCooldown",
            1.5f,
            new ConfigDescription(
                "Real-time seconds to wait after an AI conversation finishes playing before the next one can start (0-20)\n" +
                "Only counted after a successful conversation\n" +
                "一轮 AI 对话播放完后，至少再等多少秒（现实时间）才能开始下一轮（0–20）\n" +
                "只在成功的对话之后计时",
                new AcceptableValueRange<float>(0f, 20f)));

        Policy.ServiceCooldown = config.Bind(
            "RuntimePolicy",
            "ServiceCooldown",
            15f,
            new ConfigDescription(
                "Real-time seconds a failed service is set aside while other services are used (0-60)\n" +
                "Each further failure in a row adds 1 second\n" +
                "服务请求失败后暂停使用它的秒数（现实时间，0–60），期间改用其他服务\n" +
                "连续失败时每次再多 1 秒",
                new AcceptableValueRange<float>(0f, 60f)));

        Policy.ToggleKey = config.Bind(
            "RuntimePolicy",
            "ToggleKey",
            KeyCode.None,
            new ConfigDescription(
                "Key that turns AI Talk on or off, using Unity KeyCode names such as F9 or KeypadPlus\n" +
                "None = no key\n" +
                "开关 AI 对话的快捷键，使用 Unity KeyCode 名称，例如 F9、KeypadPlus\n" +
                "None 为不设快捷键"));

        Policy.PlayerTalkTrigger = config.Bind(
            "RuntimePolicy",
            "PlayerTalkTrigger",
            true,
            new ConfigDescription(
                "Start an AI conversation right away after you say something with the chat key, ignoring cooldowns\n" +
                "When off, your line is only shown as a bubble\n" +
                "用聊天键说话后立即发起一轮 AI 对话，不受冷却限制\n" +
                "关闭后，你说的话只显示为气泡"));

        Context.DisabledProviders = config.Bind(
            "Context",
            "DisabledProviders",
            "",
            "Context sections that are not sent to the AI, separated by commas, e.g. nearby_things,recent_action_log\n" +
            "Names: zone_data, player_data, nearby_characters, nearby_things, npc_memories, recent_action_log, scene_triggers, " +
            "and inside the character data: background, character_data, relationship. Names added by other mods work too\n" +
            "不发给 AI 的上下文，用英文逗号分隔，例如 nearby_things,recent_action_log\n" +
            "可填：zone_data, player_data, nearby_characters, nearby_things, npc_memories, recent_action_log, scene_triggers，" +
            "以及角色信息里的 background, character_data, relationship；其他模组添加的上下文也可以按名称填写");

        Context.GameLogDepth = config.Bind(
            "Context",
            "GameLogDepth",
            20,
            new ConfigDescription(
                "How many recent game log lines are sent to the AI (0-100), 0 = none\n" +
                "With Memory on, talks in the log are left out here and sent by the memory system instead\n" +
                "发给 AI 的最近游戏日志条数（0–100），0 为不发送\n" +
                "开启记忆后，日志里的对话不计入这里，改由记忆系统发送",
                new AcceptableValueRange<int>(0, 100)));

        Context.NearbyRadius = config.Bind(
            "Context",
            "NearbyRadius",
            4,
            new ConfigDescription(
                "Radius in tiles around the player (0-12). Characters and items in range are sent to the AI, " +
                "and lines said in range can start an AI conversation\n" +
                "A bigger radius includes more characters and uses more tokens\n" +
                "以玩家为中心的半径格数（0–12），范围内的角色和物品会发给 AI，范围内说出的台词也会触发 AI 对话\n" +
                "半径越大，包含的角色越多，Token 消耗也越多",
                new AcceptableValueRange<int>(0, 12)));

        Context.NearbyMaxCount = config.Bind(
            "Context",
            "NearbyMaxCount",
            8,
            new ConfigDescription(
                "Maximum number of nearby characters sent to the AI, party members and important characters first (0-20)\n" +
                "最多发给 AI 的附近角色数（0–20），队友和重要角色优先",
                new AcceptableValueRange<int>(0, 20)));

        Context.NearbyImportantOnly = config.Bind(
            "Context",
            "NearbyImportantOnly",
            false,
            "Only important characters (your faction, plus unique or global NPCs that are not animals) take part in AI Talk\n" +
            "Everyone else keeps their vanilla lines and is not sent to the AI\n" +
            "只让重要角色（你的同伴，以及非动物的唯一或全局 NPC）参与 AI 对话\n" +
            "其他角色照常说原版台词，也不会发给 AI");

        Context.WhitelistMode = config.Bind(
            "Context",
            "WhitelistMode",
            false,
            "Only characters checked in the Character Filter tab take part in AI Talk. Everyone else keeps their vanilla lines\n" +
            "只有在「角色名单」页勾选的角色才参与 AI 对话，其他角色照常说原版台词");

        Context.EnableLocalizer = config.Bind(
            "Context",
            "EnableLocalizer",
            false,
            "Use the game language for the field names in the context sent to the AI. This makes the request longer\n" +
            "发给 AI 的上下文字段名改用游戏语言，请求会变长");

        Memory.Enabled = config.Bind(
            "Memory",
            "Enabled",
            false,
            "NPC memory: important characters remember recent talks (short-term memory)\n" +
            "Long-term memory is only summarized automatically for characters with " +
            "\"Auto-summarize long-term memory\" checked in the Backgrounds tab\n" +
            "When off, talks are sent with the recent game log instead\n" +
            "NPC 记忆：重要角色会记住最近的对话（短期记忆）\n" +
            "只有在「角色背景」页勾选了「自动总结长期记忆」的角色，才会自动总结出长期记忆\n" +
            "关闭时，对话改为随最近游戏日志一起发送");

        Memory.MaxStmEntries = config.Bind(
            "Memory",
            "MaxStmEntries",
            12,
            new ConfigDescription(
                "Short-term memory entries kept per character (4-60), the oldest are dropped first\n" +
                "每个角色保留的短期记忆条数（4–60），超出时先删最早的",
                new AcceptableValueRange<int>(4, 60)));

        Memory.MaxStmEntriesAfterSummarization = config.Bind(
            "Memory",
            "MaxStmEntriesAfterSummarization",
            4,
            new ConfigDescription(
                "Short-term memory entries kept after a summary (2-30), must be lower than MaxStmEntries\n" +
                "总结后保留的短期记忆条数（2–30），必须小于 MaxStmEntries",
                new AcceptableValueRange<int>(2, 30)));

        Memory.MaxStmInContext = config.Bind(
            "Memory",
            "MaxStmInContext",
            5,
            new ConfigDescription(
                "Short-term memory entries per character sent in one request (1-30)\n" +
                "每次请求为每个角色发送的短期记忆条数（1–30）",
                new AcceptableValueRange<int>(1, 30)));

        Memory.MaxStmRepeatInContext = config.Bind(
            "Memory",
            "MaxStmRepeatInContext",
            2,
            new ConfigDescription(
                "How many requests in a row the same short-term memory entry can be sent in (1-10)\n" +
                "1 = each entry is sent once, and the count starts over with each new talk. Higher values keep old entries around longer\n" +
                "同一条短期记忆最多连续出现在几次请求中（1–10）\n" +
                "设为 1 时每条只发送一次，有新对话后重新计数；值越大，旧记忆停留越久",
                new AcceptableValueRange<int>(1, 10)));

        Memory.MaxLtmInContext = config.Bind(
            "Memory",
            "MaxLtmInContext",
            5,
            new ConfigDescription(
                "Long-term memory facts per character sent in one request, higher ★ first (1-30)\n" +
                "每次请求为每个角色发送的长期记忆条数，★ 高的优先（1–30）",
                new AcceptableValueRange<int>(1, 30)));

        Memory.MaxLtmEntries = config.Bind(
            "Memory",
            "MaxLtmEntries",
            20,
            new ConfigDescription(
                "Long-term memory facts stored per character (5-50)\n" +
                "When full, facts with low ★, few recalls and no recent use are forgotten first. " +
                "The 5 oldest facts with ★4 or more are always kept\n" +
                "每个角色最多保存的长期记忆条数（5–50）\n" +
                "存满后，先遗忘 ★ 低、很少被提起、很久没用到的条目；★4 及以上最早的 5 条始终保留",
                new AcceptableValueRange<int>(5, 50)));

        Memory.SummarizeThresholdPercentage = config.Bind(
            "Memory",
            "SummarizeThresholdPercentage",
            0.75f,
            new ConfigDescription(
                "Summarize automatically when short-term memory reaches this fraction of MaxStmEntries (0.25-1), 0.75 = 75%\n" +
                "短期记忆达到 MaxStmEntries 的这个比例时自动总结（0.25–1），0.75 即 75%",
                new AcceptableValueRange<float>(0.25f, 1f)));

        Memory.SummarizeThresholdSeconds = config.Bind(
            "Memory",
            "SummarizeThresholdSeconds",
            120,
            new ConfigDescription(
                "Minimum real-time seconds between two automatic summaries of the same character (10-600)\n" +
                "同一角色两次自动总结之间至少间隔的秒数（现实时间，10–600）",
                new AcceptableValueRange<int>(10, 600)));

        Scene.MaxReactions = config.Bind(
            "Scene",
            "MaxReactions",
            4,
            new ConfigDescription(
                "Maximum lines the AI can write in one AI conversation (1-8)\n" +
                "More lines can involve more characters, but take longer and may drift off topic\n" +
                "一轮 AI 对话最多几句台词（1–8）\n" +
                "句数多可以让更多角色参与，但耗时更长，也更容易跑题",
                new AcceptableValueRange<int>(1, 8)));

        Scene.TurnsCooldown = config.Bind(
            "Scene",
            "TurnsCooldown",
            12,
            "Game turns a character must wait after speaking before it can speak again (0 or more)\n" +
            "During the cooldown, its vanilla lines nearby are skipped too. Each character is counted separately\n" +
            "角色说话后，至少隔多少游戏回合才能再说话（0 或以上）\n" +
            "冷却期间，它在附近说的原版台词也会被省略；每个角色单独计算");

        Scene.SecondsCooldown = config.Bind(
            "Scene",
            "SecondsCooldown",
            5f,
            new ConfigDescription(
                "Real-time seconds a character must wait after speaking before it can speak again (0-60)\n" +
                "Works together with TurnsCooldown: both must pass. Each character is counted separately\n" +
                "角色说话后，至少隔多少秒（现实时间）才能再说话（0–60）\n" +
                "与 TurnsCooldown 同时生效，两者都结束才行；每个角色单独计算",
                new AcceptableValueRange<float>(0f, 60f)));

        Scene.TurnsIdleTrigger = config.Bind(
            "Scene",
            "TurnsIdleTrigger",
            12,
            "Game turns after the last AI conversation before a new one starts on its own, if anyone is nearby\n" +
            "-1 = off\n" +
            "距上一轮 AI 对话多少游戏回合后，只要附近有人，就自动开始一轮 AI 对话\n" +
            "设为 -1 关闭");

        Scene.SceneBufferWindow = config.Bind(
            "Scene",
            "SceneBufferWindow",
            0.1f,
            new ConfigDescription(
                "Seconds to collect lines said at almost the same time into one AI conversation (0-1)\n" +
                "Keeps everyone from talking at once, e.g. right after loading a save. Leave it as is unless asked to change it\n" +
                "把几乎同时说出的台词收进同一轮 AI 对话的等待秒数（0–1）\n" +
                "可以避免刚读档时所有人同时说话；除非有人让你改，否则保持默认",
                new AcceptableValueRange<float>(0f, 1f)));

        Scene.BlockCharaTalk = config.Bind(
            "Scene",
            "BlockCharaTalk",
            true,
            "Hide a character's vanilla lines while it is waiting for an AI reply\n" +
            "With a long Timeout, all its vanilla lines during the wait are skipped\n" +
            "角色等待 AI 回复期间，隐藏它的原版台词\n" +
            "Timeout 设得很长时，等待期间的原版台词都会被省略");

        Scene.PrefixSpeakerName = config.Bind(
            "Scene",
            "PrefixSpeakerName",
            true,
            "In the message log, show the speaker's name in front of each line from AI Talk and chat\n" +
            "在消息日志里，AI 对话和聊天台词前面显示说话角色的名字");

        Scene.MinimalReactionDelay = config.Bind(
            "Scene",
            "MinimalReactionDelay",
            0.5f,
            new ConfigDescription(
                "Minimum seconds between two lines of one AI conversation (0-10)\n" +
                "Models tend to give every line the same delay, so at 0 they may all pop up at once\n" +
                "一轮 AI 对话中两句台词之间的最短间隔秒数（0–10）\n" +
                "模型常给每句相同的延迟，设为 0 时可能同时弹出",
                new AcceptableValueRange<float>(0f, 10f)));

        Reload();
    }

    internal static class Scene
    {
        internal static ConfigEntry<int> MaxReactions { get; set; } = null!;
        internal static ConfigEntry<int> TurnsIdleTrigger { get; set; } = null!;
        internal static ConfigEntry<float> SceneBufferWindow { get; set; } = null!;
        internal static ConfigEntry<bool> BlockCharaTalk { get; set; } = null!;
        internal static ConfigEntry<int> TurnsCooldown { get; set; } = null!;
        internal static ConfigEntry<float> SecondsCooldown { get; set; } = null!;
        internal static ConfigEntry<bool> PrefixSpeakerName { get; set; } = null!;
        internal static ConfigEntry<float> MinimalReactionDelay { get; set; } = null!;
    }

    internal static class Context
    {
        internal static ConfigEntry<string> DisabledProviders { get; set; } = null!;
        internal static ConfigEntry<int> GameLogDepth { get; set; } = null!;
        internal static ConfigEntry<int> NearbyRadius { get; set; } = null!;
        internal static ConfigEntry<int> NearbyMaxCount { get; set; } = null!;
        internal static ConfigEntry<bool> NearbyImportantOnly { get; set; } = null!;
        internal static ConfigEntry<bool> WhitelistMode { get; set; } = null!;
        internal static ConfigEntry<bool> EnableLocalizer { get; set; } = null!;
    }

    internal static class Memory
    {
        internal static ConfigEntry<bool> Enabled { get; set; } = null!;
        internal static ConfigEntry<int> MaxStmEntries { get; set; } = null!;
        internal static ConfigEntry<int> MaxStmEntriesAfterSummarization { get; set; } = null!;
        internal static ConfigEntry<int> MaxStmInContext { get; set; } = null!;
        internal static ConfigEntry<int> MaxStmRepeatInContext { get; set; } = null!;
        internal static ConfigEntry<int> MaxLtmInContext { get; set; } = null!;
        internal static ConfigEntry<int> MaxLtmEntries { get; set; } = null!;
        internal static ConfigEntry<float> SummarizeThresholdPercentage { get; set; } = null!;
        internal static ConfigEntry<int> SummarizeThresholdSeconds { get; set; } = null!;
    }

    internal static class Policy
    {
        internal static ConfigEntry<float> Timeout { get; set; } = null!;
        internal static ConfigEntry<int> Retries { get; set; } = null!;
        internal static ConfigEntry<int> ConcurrentRequests { get; set; } = null!;
        internal static ConfigEntry<bool> Verbose { get; set; } = null!;
        internal static ConfigEntry<float> ServiceCooldown { get; set; } = null!;
        internal static ConfigEntry<float> GlobalRequestCooldown { get; set; } = null!;
        internal static ConfigEntry<KeyCode> ToggleKey { get; set; } = null!;
        internal static ConfigEntry<bool> PlayerTalkTrigger { get; set; } = null!;
    }
}