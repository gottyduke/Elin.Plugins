using System;

namespace Emmersive.API.Profiles;

public class CharaProfile(int uid)
{
    public Chara? GlobalChara => EClass.game.cards.Find(uid);
    public DateTime LastReactionTime { get; set; } = DateTime.MinValue;
    public int LastReactionTurn { get; set; } = int.MinValue;
    public string? ExtraData { get; set; } = null;

    public bool CanTalk =>
        (DateTime.Now - LastReactionTime).TotalSeconds > EmConfig.Scene.SecondsCooldown.Value &&
        GlobalChara!.turn - LastReactionTurn > EmConfig.Scene.TurnsCooldown.Value;
}