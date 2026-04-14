using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class QuestCreateDelta : ElinDelta
{
    [Key(0)]
    public required LZ4Bytes Data { get; init; }

    [Key(1)]
    public required bool AssignQuest { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (NetSession.Instance.IsClient) {
            return;
        }

        var quest = Data.Decompress<Quest>();
        if (quest.person.chara is not Chara chara) {
            return;
        }

        quest.SetClient(chara, AssignQuest);
    }

    public static QuestCreateDelta Create(Quest quest) {
        return new QuestCreateDelta {
            Data = LZ4Bytes.Create(quest),
            AssignQuest = quest.person.chara.quest == quest,
        };
    }
}