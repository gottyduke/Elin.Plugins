using System.Collections.Generic;
using ElinTogether.Helper;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class QuestCreateDelta : ElinDelta
{
    [IgnoreMember]
    public int Uid;

    [Key(0)]
    public required LZ4Bytes Data { get; set; }

    [Key(1)]
    public required bool AssignQuest { get; set; }

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

    public static QuestCreateDelta Create(Quest quest)
    {
        return new QuestCreateDelta {
            Uid = quest.uid,
            Data = LZ4Bytes.Create(quest),
            AssignQuest = quest.person.chara.quest == quest,
        };
    }

    internal static void Refresh(List<ElinDelta> deltaList)
    {
        var alreadySent = new List<int>();
        deltaList.RemoveAll(delta => {
            if (delta is not QuestCreateDelta questCreateDelta) {
                return false;
            }

            var quest = game.quests.list.Find(q => q.uid == questCreateDelta.Uid);
            if (quest is null) {
                return true;
            }

            alreadySent.Add(quest.uid);
            questCreateDelta.Data = LZ4Bytes.Create(quest);
            questCreateDelta.AssignQuest = quest.person.chara.quest == quest;
            return false;
        });

        deltaList.RemoveAll(delta => {
            if (delta is not QuestSetClientDelta questSetClientDelta) {
                return false;
            }

            return alreadySent.Contains(questSetClientDelta.Uid);
        });
    }
}