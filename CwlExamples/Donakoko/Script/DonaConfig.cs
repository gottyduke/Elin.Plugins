using BepInEx.Configuration;

namespace Dona;

internal class DonaConfig
{
    internal static ConfigEntry<int>? ImageChance { get; private set; }
    internal static ConfigEntry<int>? ImageDuration { get; private set; }
    internal static ConfigEntry<int>? ImageLimit { get; private set; }

    internal static void Load(ConfigFile config)
    {
        ImageChance = config.Bind(
            ModInfo.Name,
            "ImageChange",
            10,
            "Chance on hit to make a photocopy image of the target, 10 = 10%\n击中目标后生成过往镜像的几率，10 = 10%");

        ImageDuration = config.Bind(
            ModInfo.Name,
            "ImageDuration",
            100,
            "Duration of the image, in turns\n过往镜像持续回合数");

        ImageLimit = config.Bind(
            ModInfo.Name,
            "ImageLimit",
            2,
            "Max amount of image at the same time\n最大同时存在的过往镜像数量");
    }
}