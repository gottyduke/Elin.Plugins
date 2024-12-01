namespace KoC;

internal class KocLoc
{
    internal static string CaughtPrompt => Lang.langCode switch {
        "CN" => "你被抓了现行！",
        "JP" => "目撃されました！",
        "ZHTW" => "你被抓了現行！",
        "KR" => "당신이 현장에서 적발되었습니다! ",
        _ => "You were caught in the act!",
    };

    internal static string RaiseSuspicion => Lang.langCode switch {
        "CN" => "几道目光落在你身上。",
        "JP" => "いくつかの視線があなたに向けられています。",
        "ZHTW" => "几道目光落在你身上。",
        "KR" => "여러 시선이 당신에게 쏠립니다. ",
        _ => "Several gazes fall upon you.",
    };

    internal static string WithWitness(int count)
    {
        var plural = count > 1 ? "es" : "";
        var be = count > 1 ? "are" : "is";
        return Lang.langCode switch {
            "CN" => $"有{count}名目击者。",
            "JP" => $"{count}人の目撃者がいます。",
            "ZHTW" => $"有{count}名目击者。",
            "KR" => $"목격자가 {count}명 있습니다.",
            _ => $"There {be} {count} witness{plural}.",
        };
    }
}