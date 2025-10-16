using UnityEngine;

namespace Emmersive.API.Profiles;

public class CharaProfile(Chara chara)
{
    public float LastReactionTime { get; set; } = -1919.810f;
    public int LastReactionTurn { get; set; } = -114514;
    public string? ExtraData { get; set; } = null;

    public bool TalkOnCooldown =>
        Time.unscaledTime - LastReactionTime <= EmConfig.Scene.SecondsCooldown.Value ||
        chara.turn - LastReactionTurn <= EmConfig.Scene.TurnsCooldown.Value;

    public bool LockedInRequest { get; set; }

    public void SetTalked()
    {
        LastReactionTime = Time.unscaledTime;
        LastReactionTurn = chara.turn;
    }
}