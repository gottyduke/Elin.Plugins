namespace Erpc.Resources;

internal static class Races
{
    internal static string GetRaceText(this SourceRace.Row race)
    {
        var en = char.ToUpper(race.name[0]) + race.name[1..];
        return LocHelper.GetLangCode() switch {
            "JP" => race.name_JP,
            "EN" => en,
            _ => string.IsNullOrWhiteSpace(race.name_L) ? en : race.name_L,
        };
    }
}