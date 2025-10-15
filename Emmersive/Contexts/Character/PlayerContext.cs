using System.Collections.Generic;

namespace Emmersive.Contexts;

public class PlayerContext() : CharaContext(EClass.pc)
{
    public override string Name => "player_data";

    protected override IDictionary<string, object> BuildInternal()
    {
        var data = base.BuildInternal();
        data["fame"] = EClass.player.fame;
        return data;
    }
}