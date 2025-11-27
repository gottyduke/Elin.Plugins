#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.quest")]
public class CustomQuest : CustomQuestStorage
{
    internal static readonly Dictionary<string, SourceQuest.Row> Managed = [];


    internal static List<MethodInfo> ExternalValidators =>
        field ??= AttributeQuery.MethodsWith<CwlQuestConditionValidator>()
            .Select(aq => aq.Item1)
            .ToList();

    public override bool RequireClientInSameZone => true;
    public override bool CanAbandon => Data.CanAbandon;
    public override int AffinityGain => 0;

    public override void OnInit()
    {
        var data = GetQuestData(id);
        if (data is not null) {
            Data = data;
        }
    }

    public override bool CanStartQuest()
    {
        if (game.quests.IsCompleted(id) && !Data.Replayable) {
            return false;
        }

        return Data.StartConditions.All(IsValidCondition);
    }

    public override void OnStart()
    {
        CwlMod.Debug<CustomQuest>($"starting: {id}, client: {chara.id}");
    }

    public override string GetTitle()
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.Title, base.GetTitle());
    }

    public override string GetTrackerText()
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.Tracker, base.GetTrackerText());
    }

    public override string GetDetail(bool onJournal = false)
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.Detail, base.GetDetail(onJournal));
    }

    public override string GetDetailText(bool onJournal = false)
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.DetailFull, base.GetDetailText(onJournal));
    }

    public override string GetTextProgress()
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.Progress, base.GetTextProgress());
    }

    public new string GetRewardText()
    {
        if (!Data.Texts.TryGetValue(QuestTextType.Reward, out var text)) {
            text = $"qReward{RewardSuffix}".lang(rewardMoney.ToString());
        }

        return text;
    }

    public override string GetTalkProgress()
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.TalkProgress, base.GetTalkProgress());
    }

    public override string GetTalkComplete()
    {
        return Data.Texts.GetValueOrDefault(QuestTextType.TalkComplete, base.GetTalkComplete());
    }

    public override void OnComplete()
    {
        game.quests.completedTypes.Remove(typeof(CustomQuest).ToString());
    }

    [OnDeserialized]
    protected void OnDeserialized(StreamingContext context)
    {
    }

    public void UpdateText(QuestTextType type, string text)
    {
        Data.Texts[type] = text;
    }

    public bool IsValidCondition(string condition, string expr)
    {
        condition = condition.Trim().ToLowerInvariant();
        switch (condition) {
            case "affinity":
                return chara is null || expr.Compare(chara._affinity);
            case "karma":
                return expr.Compare(player.karma);
            case "zone":
                return expr == pc.currentZone.id;
        }

        var payload = condition.Split('.')[^1];

        // flag check
        if (condition.StartsWith("flag")) {
            return expr.Compare(pc.GetFlagValue(payload));
        }

        // currency check
        if (condition.StartsWith("money")) {
            var currency = payload switch {
                "bank" => player.bankMoney,
                _ => pc.GetCurrency(payload),
            };

            return expr.Compare(currency);
        }

        // lv check
        if (condition.StartsWith("lv")) {
            var constraint = payload switch {
                "pc" => pc.LV,
                "party" when pc.party.members is { Count: > 0 } party => (int)party.Average(c => c.LV),
                _ => chara?.LV,
            };

            return constraint is null || expr.Compare(constraint.Value);
        }

        var validators = ExternalValidators.ToArray();
        foreach (var method in validators) {
            try {
                var result = method.FastInvokeStatic(this, condition, expr);
                if (result is true) {
                    return true;
                }
            } catch (Exception ex) {
                ExternalValidators?.Remove(method);
                CwlMod.Warn<CustomQuest>($"external validator failed for {condition} - {expr}, removed\n{ex}");
                // noexcept
            }
        }

        CwlMod.Warn<CustomQuest>($"unable to parse condition {condition} - {expr}, ignored and set to true");

        return true;
    }

    public bool IsValidCondition(KeyValuePair<string, string> conditionPair)
    {
        return IsValidCondition(conditionPair.Key, conditionPair.Value);
    }

    public static SerializableQuestData? GetQuestData(string id)
    {
        return PackageIterator.GetJsonsFromPackage<SerializableQuestData>($"Data/quest_{id}.json").LastOrDefault().Item2;
    }

    public static CustomQuest? Get(string id)
    {
        if (!core.IsGameStarted) {
            return null;
        }

        return game.quests.globalList
            .OfType<CustomQuest>()
            .FirstOrDefault(q => q.id == id);
    }

    // TODO loc
    [CwlSceneInitEvent(Scene.Mode.StartGame, true, order: CwlSceneEventOrder.QuestImporter)]
    internal static void AddDelayedQuest()
    {
        var qm = game.quests;
        foreach (var (id, r) in Managed) {
            if (Get(id) is not null) {
                CwlMod.Log<CustomQuest>($"skipped quest {id}, already exist");
                continue;
            }

            var data = GetQuestData(id);
            if (data is null) {
                CwlMod.Log<CustomQuest>($"skipped quest {id}, unable to find valid quest data definition");
                continue;
            }

            if (qm.IsCompleted(id) && !data.Replayable) {
                CwlMod.Log<CustomQuest>($"skipped quest {id}, already completed & not replayable");
                continue;
            }

            var client = pc;
            if (r.drama.Length == 0) {
                CwlMod.Log<CustomQuest>($"quest {id} has no drama defined, maybe script generated");
            } else {
                client = game.cards.globalCharas.Find(r.drama[0]) ?? pc;
            }

            var quest = new CustomQuest {
                id = id,
            };

            quest.Init();
            quest.SetClient(client, false);

            qm.globalList.Add(quest);
            CwlMod.Log<CustomQuest>($"added quest {id} with client {client.Name}");
        }
    }
}
#endif