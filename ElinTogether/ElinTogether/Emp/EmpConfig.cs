using BepInEx.Configuration;
using ReflexCLI.Attributes;

namespace ElinTogether;

[ConsoleCommandClassCustomizer("emp")]
internal partial class EmpConfig
{
    internal static void Bind()
    {
        var config = EmpMod.Instance.Config;

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
                "Timeout in seconds for any requests\n" +
                "Retry attempts will not be made after timeout\n" +
                "网络请求的最大超时\n" +
                "超时后，将不会重新请求",
                new AcceptableValueRange<float>(1f, 60f)));

        Policy.Retries = config.Bind(
            "RuntimePolicy",
            "Retries",
            1,
            new ConfigDescription(
                "Retries attempts after a failed request\n" +
                "请求失败后的重试次数",
                new AcceptableValueRange<int>(0, 5)));

        Server.ExtraSourceValidation = config.Bind(
            "Server",
            "SourceValidation",
            "",
            "Extra source validation sets\n" +
            "额外的源表校验类型");

        Server.SharedAverageSpeed = config.Bind(
            "Server",
            "SharedAverageSpeed",
            true,
            "Share an averaged speed for all players\n" +
            "Otherwise each player will have their own speed\n" +
            "所有玩家共享平均速度\n" +
            "否则所有人按各自速度行动");

        Reload();
    }

    internal static class Policy
    {
        internal static ConfigEntry<float> Timeout { get; set; } = null!;
        internal static ConfigEntry<int> Retries { get; set; } = null!;
        internal static ConfigEntry<bool> Verbose { get; set; } = null!;
    }

    internal static class Server
    {
        internal static ConfigEntry<string> ExtraSourceValidation { get; set; } = null!;
        internal static ConfigEntry<bool> SharedAverageSpeed { get; set; } = null!;
    }
}