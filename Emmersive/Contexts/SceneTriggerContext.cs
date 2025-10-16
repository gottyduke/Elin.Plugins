using System.Collections.Generic;
using System.Linq;
using Emmersive.Components;

namespace Emmersive.Contexts;

public class SceneTriggerContext(IEnumerable<SceneTriggerEvent> triggers) : ContextProviderBase
{
    public override string Name => "scene_triggers";

    public override object? Build()
    {
        var sceneTriggers = triggers
            .Select(t => t.TransformContext())
            .ToArray();

        return sceneTriggers.Length == 0
            ? null
            : sceneTriggers;
    }
}