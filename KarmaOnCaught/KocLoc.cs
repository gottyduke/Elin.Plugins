namespace KoC;

internal class KocLoc
{
    internal static string CaughtPrompt => Lang.langCode switch {
        "CN" => "你被抓了现行！",
        "EN" => "You were caught in the act!",
        "JP" => "目撃されました！",
        _ => "",
    };
}