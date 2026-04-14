using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class QuestStartDelta : ElinDelta
{
    [Key(0)]
    public required int Uid { get; init; }

    [Key(1)]
    public required RemoteCard? Owner { get; init; }
    
    [Key(2)]
    public required LZ4Bytes? Data { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Data?.Decompress<Quest>() is Quest q) {
            game.quests.Start(q);
            return;
        }

        if (Owner?.Find() is not Chara owner) {
            return;
        }

        var quest = owner.quest;
        if (quest.uid != Uid || game.quests.list.Contains(quest)) {
            return;
        }

        game.quests.Start(quest);
    }
}