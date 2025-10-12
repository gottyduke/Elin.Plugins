using System.Collections.Generic;
using System.Linq;
using Emmersive.API.Plugins.SceneScheduler;

namespace Emmersive.Contexts;

public class SceneTriggerContext(IEnumerable<SceneTriggerEvent> triggers) : ContextProviderBase
{
    public override string Name => "scene_triggers";

    public override object Build()
    {
        return triggers
            .Select(t => t.TransformContext())
            .ToArray();
    }
}