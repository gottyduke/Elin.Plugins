using System.Collections.Generic;
using Cwl.API.Processors;
using MethodTimer;
using Newtonsoft.Json;

namespace Cwl.API.Custom;

public class CustomReligion(string religionId) : Religion
{
    private static readonly Dictionary<string, CustomReligion> _cached = [];
    private static bool _applied;
    private bool _canJoin;

    [JsonProperty] private string _id = religionId;
    private bool _isMinor;

    public override string id => _id;
    public override bool IsMinorGod => _isMinor;
    public override bool CanJoin => _canJoin;

    public static IEnumerable<CustomReligion> All => _cached.Values;

    public static CustomReligion GerOrAdd(string id)
    {
        if (!_applied) {
            GameIOProcessor.AddSave(SaveCustomReligion, false);
            GameIOProcessor.AddLoad(LoadCustomReligion, true);
        }

        _applied = true;

        _cached.TryAdd(id, new(id));
        return _cached[id];
    }

    public CustomReligion SetMinor(bool minorGod)
    {
        _isMinor = minorGod;
        return this;
    }

    public CustomReligion SetCanJoin(bool canJoin)
    {
        _canJoin = canJoin;
        return this;
    }

    [Time]
    internal static void SaveCustomReligion(GameIOProcessor.GameIOContext context)
    {
        if (player?.chara?.faith is not CustomReligion custom) {
            return;
        }

        context.Save(custom);
    }

    [Time]
    internal static void LoadCustomReligion(GameIOProcessor.GameIOContext context)
    {
        if (player?.chara?.faith is not CustomReligion custom) {
            return;
        }

        if (!context.Load<CustomReligion>(out var loaded) ||
            loaded is null ||
            loaded._id != custom.id) {
            return;
        }

        custom.giftRank = loaded.giftRank;
        custom.mood = loaded.mood;
        custom.relation = loaded.relation;
    }
}