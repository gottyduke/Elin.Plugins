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
[Union(210, typeof(CharaBuildDelta))]
[Union(211, typeof(CharaProgressBeginDelta))]
[Union(212, typeof(CharaProgressCompleteDelta))]
[Union(213, typeof(CharaTaskCancelDelta))]
[Union(214, typeof(CharaHitFishDelta))]
// Thing
[Union(300, typeof(ThingDelta))]
// Zone
[Union(400, typeof(SpatialGenDelta))]
[Union(401, typeof(ZoneAddCardDelta))]
// World
[Union(500, typeof(GameDelta))]
public abstract class ElinDeltaBase : EClass
{
    public abstract void Apply(ElinNetBase net);
}