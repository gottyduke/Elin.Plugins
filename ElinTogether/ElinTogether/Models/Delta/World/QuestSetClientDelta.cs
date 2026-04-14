using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class QuestSetClientDelta : ElinDelta
{
    [Key(0)]
    public required int Uid { get; init; }

    [Key(1)]
    public required RemoteCard? Owner { get; init; }

    [Key(2)]
    public required RemoteCard NewChara { get; init; }

    [Key(3)]
    public bool AssignQuest { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (NewChara.Find() is not Chara chara) {
            return;
        }
        
        var quest = game.quests.list.Find(q => q.uid == Uid);
        if (quest is not null) {
            quest.SetClient(chara, AssignQuest);
            return;
        }

        if (Owner?.Find() is not Chara owner) {
            return;
        }

        quest = chara.quest;
        if (quest.uid == Uid) {
            quest.SetClient(owner, AssignQuest);
        }
    }
}