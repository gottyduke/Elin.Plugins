using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using MethodTimer;
using Newtonsoft.Json;

namespace Cwl.API.Custom;

public class CustomReligion(string religionId) : Religion, IChunkable
{
    internal static readonly Dictionary<string, CustomReligion> Managed = [];

    private bool _canJoin;
    private string[] _elements = [];

    [JsonProperty] private string _id = religionId;

    private bool _isMinor;
    private Dictionary<string, int> _offeringMtp = [];

    public override string id => _id;
    public override bool IsMinorGod => _isMinor;
    public override bool CanJoin => _canJoin;

    public static IReadOnlyCollection<CustomReligion> All => Managed.Values;
    public string FeatGodAlias => $"featGod_{_id}1";

    public string ChunkName => $"{typeof(CustomReligion).FullName}.{_id}";

    public static CustomReligion GerOrAdd(string id)
    {
        return Managed.GetOrCreate(id, () => new(id));
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

    public CustomReligion SetElements(string[] elements)
    {
        _elements = elements;
        return this;
    }

    public CustomReligion SetOfferingMtp(Dictionary<string, int> mtp)
    {
        _offeringMtp = mtp;
        return this;
    }

    public bool IsFactionElement(string alias)
    {
        return _elements.Contains(alias);
    }

    public bool IsFactionElement(Element element)
    {
        return IsFactionElement(element.source.alias);
    }

    public void Reset()
    {
        giftRank = 0;
        mood = 0;
    }

    public override int GetOfferingMtp(Thing t)
    {
        return _offeringMtp.GetValueOrDefault(t.id, base.GetOfferingMtp(t));
    }

    [Time]
    [CwlPostSave]
    internal static void SaveCustomReligion(GameIOProcessor.GameIOContext? context)
    {
        if (context is null) {
            return;
        }

        var religions = game.religions.list
            .OfType<CustomReligion>()
            .ToDictionary(r => r.id, r => r);
        context.Save(religions, "custom_religions");
    }

    [Time]
    [CwlPostLoad]
    internal static void LoadCustomReligion(GameIOProcessor.GameIOContext? context)
    {
        if (context is null) {
            return;
        }

        if (context.Load<Dictionary<string, CustomReligion>>(out var religions, "custom_religions") &&
            religions is not null) {
            foreach (var custom in game.religions.list.OfType<CustomReligion>()) {
                if (!religions.TryGetValue(custom.id, out var loaded)) {
                    continue;
                }

                custom.giftRank = loaded.giftRank;
                custom.mood = loaded.mood;
                custom.relation = loaded.relation;

                // TODO: remove deprecated code after <5> versions
                context.Remove(custom.ChunkName);
            }
        } else {
            // TODO: remove deprecated code after <5> versions
            foreach (var custom in game.religions.list.OfType<CustomReligion>()) {
                if (!context.Load<CustomReligion>(out var loaded, custom.ChunkName) ||
                    loaded?._id != custom.id) {
                    continue;
                }

                custom.giftRank = loaded.giftRank;
                custom.mood = loaded.mood;
                custom.relation = loaded.relation;
            }
        }
    }

    [CwlActPerformEvent]
    private static void ProcGodTalk(Act act)
    {
        if (!CustomElement.Managed.ContainsKey(act.id)) {
            return;
        }

        foreach (var (id, religion) in Managed) {
            if (act.HasTag(id)) {
                religion.Talk("ability");
            }
        }
    }
}