using UnityEngine;

namespace Emmersive.API.Profiles;

public class CharaProfile(Chara chara)
{
    public float LastReactionTime { get; set; } = -1919.810f;
    public int LastReactionTurn { get; set; } = -114514;

    public bool OnTalkCooldown =>
        Time.unscaledTime - LastReactionTime <= EmConfig.Scene.SecondsCooldown.Value ||
        chara.turn - LastReactionTurn <= EmConfig.Scene.TurnsCooldown.Value;

    public bool LockedInRequest { get; set; }
    public bool OnWhitelist => chara.GetBool("em_wl");
    public bool OnBlacklist => chara.GetBool("em_bl");
    public bool UsePopFeed => chara.GetBool("em_pop");

    public bool IsImportant =>
        !chara.IsPC &&
        (chara.IsPCFaction || (!chara.IsAnimal && (chara.IsUnique || chara.IsGlobal)));

    public bool CanTrigger =>
        IsPC ||
        ((!EmConfig.Context.NearbyImportantOnly.Value || IsImportant) &&
         (!EmConfig.Context.WhitelistMode.Value || OnWhitelist) &&
         !OnBlacklist);

    public bool IsPC => chara.IsPC;

    public void ResetTalkCooldown()
    {
        LastReactionTime = Time.unscaledTime;
        LastReactionTurn = chara.turn;
    }
}