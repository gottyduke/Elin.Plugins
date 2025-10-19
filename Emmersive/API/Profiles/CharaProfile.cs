using System.Collections.Generic;
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

    public List<string> LastTalks { get; set; } = [];

    public void ResetTalkCooldown(string? talk = null)
    {
        LastReactionTime = Time.unscaledTime;
        LastReactionTurn = chara.turn;

        if (talk is null) {
            return;
        }

        LastTalks.Add(talk);

        if (LastTalks.Count > EmConfig.Context.RecentLogDepth.Value + 1) {
            LastTalks.RemoveAt(0);
        }
    }
}