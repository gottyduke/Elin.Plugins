using BepInEx.Configuration;

namespace CustomizerMinus;

internal class CmmConfig
{
    internal static ConfigEntry<int> MaxPartsPerRow { get; private set; } = null!;
    internal static ConfigEntry<int> PartCellWidth { get; private set; } = null!;
    internal static ConfigEntry<int> PartCellHeight { get; private set; } = null!;
    internal static ConfigEntry<bool> EnableSliderIcon { get; private set; } = null!;
    internal static ConfigEntry<float> SliderIconScale { get; private set; } = null!;
    internal static ConfigEntry<float> RotateButtonScale { get; private set; } = null!;

    internal static void Load(ConfigFile config)
    {
        MaxPartsPerRow = config.Bind(
            ModInfo.Name,
            "MaxPartsPerRow",
            4,
            "Maximum allowed part previews displayed on the same row\n" +
            "同一行允许显示的最大部件预览数");

        PartCellWidth = config.Bind(
            ModInfo.Name,
            "PartCellWidth",
            128,
            new ConfigDescription(
                "Width of the part preview in the parts picker window\n" +
                "部件预览图的宽度",
                new AcceptableValueRange<int>(64, 192)));

        PartCellHeight = config.Bind(
            ModInfo.Name,
            "PartCellHeight",
            192,
            new ConfigDescription(
                "Height of the part preview in the parts picker window\n" +
                "部件预览图的长度",
                new AcceptableValueRange<int>(96, 288)));
        
        EnableSliderIcon = config.Bind(
            ModInfo.Name,
            "EnableSliderIcon",
            true,
            "Replace the slider buttons with custom textures\n" +
            "替换两个滑条方块的贴图");
        
        SliderIconScale = config.Bind(
            ModInfo.Name,
            "SliderIconScale",
            1f,
            new ConfigDescription(
                "Scaling ratio of custom slider textures\n" +
                "滑条自定义贴图的大小缩放",
                new AcceptableValueRange<float>(0.5f, 2f)));

        RotateButtonScale = config.Bind(
            ModInfo.Name,
            "RotateButtonScale",
            2f,
            new ConfigDescription(
                "Scaling ratio of rotate buttons\n" +
                "旋转按钮的大小缩放",
                new AcceptableValueRange<float>(1f, 5f)));
    }
}