using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.LangMod;
using MethodTimer;
using Newtonsoft.Json;

namespace Cwl.API.Custom;

public class CustomReligion : Religion, IChunkable
{
    internal static readonly Dictionary<string, CustomReligion> Managed = [];

    private bool _canJoin;
    private string[] _elements = [];

    [JsonProperty]
    private string _id = "";

    private bool _isMinor;
    private Dictionary<string, int> _offeringMtp = [];

    public override string id => _id;
    public override bool IsMinorGod => _isMinor;
    public override bool CanJoin => _canJoin;

    public static IReadOnlyCollection<CustomReligion> All => Managed.Values;
    public string FeatGodAlias => $"featGod_{_id}1";
    public string ChunkName => $"{typeof(CustomReligion).FullName}.{_id}";

    public static CustomReligion GerOrAdd(string id, string? type = null)
    {
        return Managed.GetOrCreate(id, SafeCreate);

        CustomReligion SafeCreate()
        {
            type ??= typeof(CustomReligion).FullName;

            CustomReligion custom;
            try {
                custom = ClassCache.Create<CustomReligion>(type, CwlMod.Assembly.FullName);
                if (custom is null) {
                    throw new InvalidCastException(type);
                }
            } catch (Exception ex) {
                CwlMod.Warn("cwl_error_failure".Loc(ex));
                custom = new();
                // noexcept
            }

            custom._id = id;
            return custom;
        }
    }

    public static void AddExternalManaged<T>(T religion) where T : CustomReligion
    {
        Managed[religion.id] = religion;
        religion._id = religion.id;
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
    internal static void SaveCustomReligion(GameIOProcessor.GameIOContext context)
    {
        var religions = game.religions.list
            .OfType<CustomReligion>()
            .ToDictionary(r => r.id);
        context.Save(religions, "custom_religions");
    }

    [Time]
    [CwlPostLoad]
    internal static void LoadCustomReligion(GameIOProcessor.GameIOContext context)
    {
        if (!context.Load<Dictionary<string, CustomReligion>>(out var religions, "custom_religions")) {
            return;
        }

        foreach (var custom in game.religions.list.OfType<CustomReligion>()) {
            if (!religions.TryGetValue(custom.id, out var loaded)) {
                continue;
            }

            custom.giftRank = loaded.giftRank;
            custom.mood = loaded.mood;
            custom.relation = loaded.relation;
        }
    }

    internal static void ResolveReligion(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return;
        }

        if (objectType != typeof(CustomReligion)) {
            return;
        }

        readType = typeof(CustomReligion);
        resolved = true;
        CwlMod.WarnWithPopup<CustomReligion>("cwl_warn_deserialize".Loc(nameof(CustomReligion), qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));
    }

    [CwlActPerformEvent]
    private static void ProcGodTalk(Act act)
    {
        if (!act.HasTag("godAbility")) {
            return;
        }

        foreach (var (id, religion) in Managed) {
            if (act.HasTag(id)) {
                religion.Talk("ability");
            }
        }
    }
}