namespace EC;

internal static class EcLoc
{
    internal static string EquippedIndicator => Lang.langCode switch {
        "CN" => "(已装备)",
        "JP" => "(装備中)",
        "ZHTW" => "(已裝備)",
        "KR" => "(장착 완료)",
        _ => "(Equipped)",
    };

    internal static string CarriedIndicator => Lang.langCode switch {
        "CN" => "(已携带)",
        "JP" => "(所持中)",
        "ZHTW" => "(已携帶)",
        "KR" => "(소지 중)",
        _ => "(Carried)",
    };

    internal static string TogglePrompt(bool on)
    {
        return Lang.langCode switch {
            "CN" => "装备比较: " + (on ? "启用 " : "禁用 "),
            "JP" => "装備比較: " + (on ? "启用 " : "無効 "),
            "ZHTW" => "裝備比較: " + (on ? "啓用 " : "禁用 "),
            "KR" => "장비 비교: " + (on ? "사용 중 " : "사용 안 함 "),
            _ => "Compare Equipment: " + (on ? "Enabled " : "Disabled "),
        };
    }
}