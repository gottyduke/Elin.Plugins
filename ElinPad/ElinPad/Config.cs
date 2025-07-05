using BepInEx.Configuration;
using ReflexCLI.Attributes;

namespace ElinPad;

[ConsoleCommandClassCustomizer("ep.config")]
internal class EpConfig
{
    [ConsoleCommand] public static bool DebuggingVerbose => Debugging.Verbose!.Value;
    [ConsoleCommand] public static bool ShowTrackInput => Debugging.TrackInput!.Value;

    [ConsoleCommand] public static float DoubleTapThreshold => Threshold.DoubleTap!.Value;

    [ConsoleCommand] public static float HoldThreshold => Threshold.Hold!.Value;

    [ConsoleCommand] public static float LeftStickDeadZone => DeadZone.LeftStick!.Value;

    [ConsoleCommand] public static float RightStickDeadZone => DeadZone.RightStick!.Value;

    [ConsoleCommand] public static float LeftTriggerDeadZone => DeadZone.LeftTrigger!.Value;

    [ConsoleCommand] public static float RightTriggerDeadZone => DeadZone.RightTrigger!.Value;

    internal static void Load(ConfigFile config)
    {
        Debugging.Verbose = config.Bind(
            ModInfo.Name,
            "Debugging.Verbose",
            false,
            "Verbose information that may be helpful(spamming) for debugging\n" +
            "Debug用的信息输出");
        Debugging.TrackInput = config.Bind(
            ModInfo.Name,
            "Debugging.TrackInput",
            false,
            "Track and display current inputs for debugging" +
            "显示输入");

#if DEBUG
        Debugging.Verbose.Value = true;
        Debugging.TrackInput.Value = true;
#endif

        Threshold.DoubleTap = config.Bind(
            ModInfo.Name,
            "Threshold.DoubleTap",
            0.3f,
            "Max time window for double tapping buttons" +
            "双击最大阈值"
        );
        Threshold.Hold = config.Bind(
            ModInfo.Name,
            "Threshold.Hold",
            0.2f,
            "Min time window for holding buttons" +
            "按住最小阈值"
        );

        DeadZone.LeftStick = config.Bind(
            ModInfo.Name,
            "DeadZone.LeftStick",
            7849f / short.MaxValue,
            new ConfigDescription(
                "Dead zone percentage for left stick" +
                "左摇杆死区比例, 0.0 - 1.0",
                new AcceptableValueRange<float>(0f, 1f))
        );
        DeadZone.RightStick = config.Bind(
            ModInfo.Name,
            "DeadZone.RightStick",
            8689f / short.MaxValue,
            new ConfigDescription(
                "Dead zone percentage for right stick" +
                "右摇杆死区比例, 0.0 - 1.0",
                new AcceptableValueRange<float>(0f, 1f))
        );
        DeadZone.LeftTrigger = config.Bind(
            ModInfo.Name,
            "DeadZone.LeftTrigger",
            30f / byte.MaxValue,
            new ConfigDescription(
                "Dead zone percentage for left trigger" +
                "左扳机死区比例, 0.0 - 1.0",
                new AcceptableValueRange<float>(0f, 1f))
        );
        DeadZone.RightTrigger = config.Bind(
            ModInfo.Name,
            "DeadZone.RightTrigger",
            30f / byte.MaxValue,
            new ConfigDescription(
                "Dead zone percentage for right trigger" +
                "右扳机死区比例, 0.0 - 1.0",
                new AcceptableValueRange<float>(0f, 1f))
        );
    }

    internal class Debugging
    {
        internal static ConfigEntry<bool>? Verbose { get; set; }
        internal static ConfigEntry<bool>? TrackInput { get; set; }
    }

    internal class Threshold
    {
        internal static ConfigEntry<float>? DoubleTap { get; set; }
        internal static ConfigEntry<float>? Hold { get; set; }
    }

    internal class DeadZone
    {
        internal static ConfigEntry<float>? LeftStick { get; set; }
        internal static ConfigEntry<float>? RightStick { get; set; }
        internal static ConfigEntry<float>? LeftTrigger { get; set; }
        internal static ConfigEntry<float>? RightTrigger { get; set; }
    }
}