using System.Collections.Generic;
using Cwl.Helper.Extensions;
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
    public bool OnWhitelist => chara.GetFlagValue("em_wl") > 0;
    public bool OnBlacklist => chara.GetFlagValue("em_bl") > 0;
    public bool IsImportant => !chara.IsAnimal && (chara.IsUnique || chara.IsGlobal);

    public bool CanTrigger =>
        (!EmConfig.Context.NearbyImportantOnly.Value || IsImportant) &&
        (!EmConfig.Context.WhitelistMode.Value || OnWhitelist) &&
        !OnBlacklist;

    public Queue<string> LastTalks { get; set; } = [];

    public void ResetTalkCooldown(string? talk = null)
    {
        LastReactionTime = Time.unscaledTime;
        LastReactionTurn = chara.turn;

        if (talk is null) {
            return;
        }

        LastTalks.Enqueue(talk);

        if (LastTalks.Count > EmConfig.Context.RecentLogDepth.Value) {
            LastTalks.Dequeue();
        }
    }
}