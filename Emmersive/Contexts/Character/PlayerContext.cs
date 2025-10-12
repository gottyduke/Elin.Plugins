using System.Collections.Generic;

namespace Emmersive.Contexts;

public class PlayerContext() : CharaContext(EClass.pc)
{
    public override string Name => "player_data";

    protected override void Localize(IDictionary<string, object> data, string? prefixOverride = null)
    {
        base.Localize(data, base.Name);
        base.Localize(data, Name);
    }

    protected override IDictionary<string, object> BuildCore()
    {
        var data = base.BuildCore();
        data["fame"] = EClass.player.fame;
        return data;
    }
}