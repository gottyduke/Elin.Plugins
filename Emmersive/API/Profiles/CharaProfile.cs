using UnityEngine;

namespace Emmersive.API.Profiles;

public class CharaProfile(Chara chara)
{
    public Chara Chara { get; internal set; } = chara;

    public float LastReactionTime { get; set; } = -1919.810f;
    public int LastReactionTurn { get; set; } = -114514;

    public bool OnTalkCooldown =>
        Time.unscaledTime - LastReactionTime <= EmConfig.Scene.SecondsCooldown.Value ||
        (Chara.turn >= LastReactionTurn && Chara.turn - LastReactionTurn <= EmConfig.Scene.TurnsCooldown.Value);

    public bool LockedInRequest { get; set; }
    public bool OnWhitelist => Chara.GetBool("em_wl");
    public bool OnBlacklist => Chara.GetBool("em_bl");
    public bool UsePopFeed => Chara.GetBool("em_pop");
    public bool AllowSummarize => Chara.GetBool("em_sum");

    public bool IsImportant =>
        !Chara.IsPC &&
        (Chara.IsPCFaction || (!Chara.IsAnimal && (Chara.IsUnique || Chara.IsGlobal)));

    public bool CanTrigger =>
        IsPC ||
        ((!EmConfig.Context.NearbyImportantOnly.Value || IsImportant) &&
         (!EmConfig.Context.WhitelistMode.Value || OnWhitelist) &&
         !OnBlacklist);

    public bool IsPC => Chara.IsPC;

    public void ResetTalkCooldown()
    {
        LastReactionTime = Time.unscaledTime;
        LastReactionTurn = Chara.turn;
    }
}