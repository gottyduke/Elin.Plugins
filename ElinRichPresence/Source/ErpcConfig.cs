using BepInEx.Configuration;

namespace Erpc;

internal static class ErpcConfig
{
    internal static ConfigEntry<string>? LangCodeOverride;
    internal static ConfigEntry<int>? UpdateTicksInterval;

    internal static void LoadConfig(ConfigFile config)
    {
        LangCodeOverride = config.Bind(
            "ERPC",
            "LangCodeOverride",
            "GAME",
            new ConfigDescription(
                "Whether or not to use a different display language for rich presence. Only supports EN/JP or GAME language",
                new AcceptableValueList<string>("GAME", "EN", "JP"))
        );

        UpdateTicksInterval = config.Bind(
            "ERPC",
            "UpdateTicksInterval",
            8,
            new ConfigDescription("Amount of ticks in between each rich presence update",
                new AcceptableValueRange<int>(1, 64))
        );
    }
}