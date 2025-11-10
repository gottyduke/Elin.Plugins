using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

// Card
[Union(100, typeof(CardGenDelta))]
[Union(101, typeof(CardDamageHpDelta))]
// Chara
[Union(200, typeof(CharaMoveDelta))]
[Union(201, typeof(CharaMoveZoneDelta))]
[Union(202, typeof(CharaMakeAllyDelta))]
[Union(203, typeof(CharaPickThingDelta))]
[Union(204, typeof(CharaDieDelta))]
// Thing
// Zone
[Union(400, typeof(ZoneAddCardDelta))]
// World
[Union(500, typeof(GameTimeDelta))]
public interface IElinDelta
{
    public void Apply(ElinNetBase net);
}