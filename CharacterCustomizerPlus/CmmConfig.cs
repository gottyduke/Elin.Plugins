using BepInEx.Configuration;

namespace CustomizerMinus;

internal class CmmConfig
{
    internal static ConfigEntry<int> MaxPartsPerRow { get; private set; } = null!;
    internal static ConfigEntry<int> PartCellWidth { get; private set; } = null!;
    internal static ConfigEntry<int> PartCellHeight { get; private set; } = null!;

    internal static void Load(ConfigFile config)
    {
        MaxPartsPerRow = config.Bind(
            ModInfo.Name,
            "MaxPartsPerRow",
            4,
            "Maximum allowed parts displayed on the same row\n" +
            "同一行允许显示的最大部件数");

        PartCellWidth = config.Bind(
            ModInfo.Name,
            "PartCellWidth",
            128,
            new ConfigDescription(
                "Width of the part cell in the parts picker window\n" +
                "部件按钮的宽度",
                new AcceptableValueRange<int>(64, 128)));

        PartCellHeight = config.Bind(
            ModInfo.Name,
            "PartCellHeight",
            192,
            new ConfigDescription(
                "Height of the part cell in the parts picker window\n" +
                "部件按钮的长度",
                new AcceptableValueRange<int>(96, 192)));
    }
}