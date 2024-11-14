// ReSharper disable StringLiteralTypo

namespace Erpc.Resources;

internal static class Zones
{
    internal static string GetBanner(this Zone z)
    {
        // @formatter:off
        return z switch {
            // Lumiest
            // Mifu
            // Home
            // Mysilia
            // Nefu
            // Noyel
            // Olvina
            // Palmia
            // Kapul
            // Specwing
            // Tinker
            // Willow
            // Yowyn
            Zone_Aquli or
            Zone_Derphy => z.idExport,
            // Zone_Arena or Zone_Arena2
            // Zone_Beach
            // Zone_Casino
            // Zone_Void
            // Zone_StartSite
            Zone_Dungeon => "default_nefia",
            Zone_Civilized => "default_town",
            Region or
            Zone_Field or 
            _ => "default_overworld",
        };
        // @formatter:on
    }

    internal static string GetZoneState(this Zone z)
    {
        var loc = "erpc_state_explore";
        if (z.IsPCFaction) {
            loc = "erpc_state_base_build";
        }

        if (z.IsRegion) {
            loc = "erpc_state_travel";
        }

        if (z is Zone_Dungeon) {
            loc = "erpc_state_nefia";
        }

        return string.Format(loc.Loc(), z.NameWithLevel);
    }
}