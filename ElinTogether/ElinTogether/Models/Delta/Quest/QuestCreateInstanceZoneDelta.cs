using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class QuestCreateInstanceZoneDelta : ElinDelta
{
    [Key(1)]
    public required int Uid { get; init; }

    [Key(2)]
    public required RemoteCard Chara { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Chara.Find() is not Chara chara) {
            return;
        }

        var quest = game.quests.list.Find(q => q.uid == Uid);
        if (quest is null) {
            return;
        }

        var zone = quest.CreateInstanceZone(chara);
        if (zone is not Zone_Wedding) {
            pc.MoveZone(zone, ZoneTransition.EnterState.Center);
            return;
        }

        pc.MoveZone(zone, new ZoneTransition {
            state = ZoneTransition.EnterState.Exact,
            x = 50,
            z = 53
        });
    }
}