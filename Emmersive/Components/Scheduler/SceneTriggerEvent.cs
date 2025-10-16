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

        Context["source_uid"] = Chara.uid;
        Context["original_text"] = Trigger;

        return Context;
    }
}