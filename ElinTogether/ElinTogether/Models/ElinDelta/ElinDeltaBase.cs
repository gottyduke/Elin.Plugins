using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

// Card
[Union(100, typeof(CardGenDelta))]
[Union(101, typeof(CardDamageHpDelta))]
[Union(102, typeof(CardPlacedDelta))]
// Chara
[Union(200, typeof(CharaMoveDelta))]
[Union(201, typeof(CharaTickDelta))]
[Union(202, typeof(CharaMakeAllyDelta))]
[Union(203, typeof(CharaPickThingDelta))]
[Union(204, typeof(CharaDieDelta))]
[Union(205, typeof(CharaActPerformDelta))]
[Union(206, typeof(CharaAddConditionDelta))]
[Union(207, typeof(CharaReviveDelta))]
[Union(208, typeof(CharaTickConditionDelta))]
[Union(209, typeof(CharaTaskDelta))]
// Thing
// Zone
[Union(400, typeof(SpatialGenDelta))]
[Union(401, typeof(ZoneAddCardDelta))]
// World
[Union(500, typeof(GameUpdateDelta))]
public abstract class ElinDeltaBase : EClass
{
    public abstract void Apply(ElinNetBase net);
}