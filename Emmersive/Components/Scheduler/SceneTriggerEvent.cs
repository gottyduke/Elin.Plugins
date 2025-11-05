using System.Collections.Generic;

namespace Emmersive.Components;

public class SceneTriggerEvent
{
    public required Chara Chara;
    public Dictionary<string, object>? Context;
    public required string Trigger;

    public object TransformContext()
    {
        Context ??= [];

        Context["uid"] = Chara.uid;
        Context["original"] = Trigger;

        return Context;
    }
}