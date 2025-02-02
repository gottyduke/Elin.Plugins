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

    public override string id => _id;
    public override bool IsMinorGod => _isMinor;
    public override bool CanJoin => _canJoin;

    public static IReadOnlyCollection<CustomReligion> All => Managed.Values;

    public string ChunkName => $"{typeof(CustomReligion).FullName}.{id}";

    public static CustomReligion GerOrAdd(string id)
    {
        Managed.TryAdd(id, new(id));
        return Managed[id];
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

    [Time]
    [CwlPreSave]
    internal static void SaveCustomReligion(GameIOProcessor.GameIOContext? context)
    {
        if (context is null) {
            return;
        }

        foreach (var custom in game.religions.list.OfType<CustomReligion>()) {
            context.Save(custom);
        }
    }

    [Time]
    [CwlPostLoad]
    internal static void LoadCustomReligion(GameIOProcessor.GameIOContext? context)
    {
        if (context is null) {
            return;
        }

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