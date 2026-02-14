using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class GameUpdateDelta : ElinDeltaBase
{
    public override void Apply(ElinNetBase net)
    {
        GameUpdaterUpdateEvent.AllowedUpdate++;
    }
}