using System.Collections.Generic;

namespace Emmersive.Contexts;

public class EnvironmentContext : ContextProviderBase
{
    public override string Name => "environment_data";

    protected override IDictionary<string, object> BuildCore()
    {
        var world = EClass.world;

        Dictionary<string, object> data = new() {
            ["date"] = $"{world.date.GetText(Date.TextFormat.Widget)} {world.date.NameTime}",
            ["season"] = world.date.month switch {
                >= 3 and <= 5 => "Spring",
                >= 6 and <= 8 => "Summer",
                >= 9 and <= 11 => "Autumn",
                12 or >= 1 and <= 2 => "Winter",
                _ => "Unknown",
            },
            ["weather"] = world.weather.GetName(),
        };

        return data;
    }
}