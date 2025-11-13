using System.Linq;
using ElinTogether.Net;
using ElinTogether.Patches;
using HarmonyLib;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaAddConditionDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    // TODO build condition type mapping
    [Key(1)]
    public required int ConditionId { get; init; }

    [Key(2)]
    public required int Power { get; init; }

    [Key(3)]
    public required bool Force { get; init; }

    [Key(4)]
    public bool Remove { get; set; }

    public override void Apply(ElinNetBase net)
    {
        if (net.IsHost) {
            // reject every single chara add condition delta from clients
            return;
        }

        if (Owner.Find() is not Chara chara) {
            return;
        }

        if (Remove) {
            chara.conditions.ForeachReverse(c => {
                if (c.id == ConditionId) {
                    c.Kill();
                }
            });
        } else {
            var row = sources.stats.map[ConditionId];
            chara.Stub_AddCondition(Condition.Create(row.alias, Power), Force);
        }
    }
}