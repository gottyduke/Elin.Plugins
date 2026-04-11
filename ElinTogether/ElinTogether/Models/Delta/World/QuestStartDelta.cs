using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class QuestStartDelta : ElinDelta
{
    [Key(0)]
    public required int Uid { get; init; }

    [Key(1)]
    public required RemoteCard RefChara { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (RefChara.Find() is not Chara chara) {
            return;
        }

        var quest = chara.quest;
        if (quest.uid != Uid || game.quests.list.Contains(quest)) {
            return;
        }

        game.quests.Start(quest);
    }
}