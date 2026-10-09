using System.Collections.Generic;

namespace Emmersive.Components;

public class SceneTriggerEvent
{
    public bool AlreadyShown;
    public required Chara Chara;
    public Dictionary<string, object>? Context;

    public bool IsPlayer;
    public required string Trigger;

    public object TransformContext()
    {
        Context ??= [];

        Context["uid"] = Chara.uid;
        Context["speaker"] = IsPlayer ? "player" : "npc";
        Context["original"] = Trigger;
        Context["already_shown"] = AlreadyShown;

        return Context;
    }
}