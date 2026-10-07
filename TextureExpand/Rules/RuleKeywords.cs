using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TextureExpand.Helper;

namespace TextureExpand.Rules;

[Flags]
internal enum Trigger
{
    None = 0,
    Move = 1 << 0,
    Battle = 1 << 1,
    Hostility = 1 << 2,
    Fur = 1 << 3,
    Zone = 1 << 4,
    Equip = 1 << 5,
    Condition = 1 << 6,
    Gender = 1 << 7,
    Tail = 1 << 8,
}

internal delegate Func<Chara, bool>? JudgeFactory(CaptureCollection args, bool expected);

internal sealed class RuleKeyword
{
    internal RuleKeyword(string keyword, JudgeFactory factory, int priority, Trigger trigger, bool allowMulti)
    {
        Keyword = keyword;
        Factory = factory;
        Priority = priority;
        Trigger = trigger;
        AllowMulti = allowMulti;
        Pattern = new($"#(not-)?{keyword}(:?-([^#-]*))*(#|$)", RegexOptions.IgnoreCase);
    }

    internal string Keyword { get; }
    internal Regex Pattern { get; }
    internal JudgeFactory Factory { get; }

    internal int Priority { get; }

    internal Trigger Trigger { get; }

    internal bool AllowMulti { get; }
}

internal static class RuleKeywords
{
    internal static readonly List<RuleKeyword> All = [];

    internal static readonly Dictionary<string, Trigger> Targets = [];

    static RuleKeywords()
    {
        Register("m", (_, expected) => c => c.IsMale == expected, 100, Trigger.Gender, false);
        Register("f", (_, expected) => c => !c.IsMale == expected, 100, Trigger.Gender, false);

        Register("eq", (args, expected) => {
            if (!HasArgs(args, "eq") || FindElement(args, "eq") is not { } element) {
                return null;
            }

            var elementId = element.id;
            var thingId = args.Count > 1 ? args[1].Value : null;
            if (thingId.IsEmpty()) {
                return c => c.body.slots.Any(s => s.elementId == elementId && s.thing is not null) == expected;
            }

            return c => c.body.slots.Any(s => s.elementId == elementId && s.thing?.id == thingId) == expected;
        }, 1, Trigger.Equip, true);

        Register("noeq", (args, expected) => {
            if (!HasArgs(args, "noeq") || FindElement(args, "noeq") is not { } element) {
                return null;
            }

            var elementId = element.id;
            return c => c.body.slots.Any(s => s.elementId == elementId && s.thing is null) == expected;
        }, 1, Trigger.Equip, true);

        Register("con", (args, expected) => {
            if (!HasArgs(args, "con")) {
                return null;
            }

            var alias = "con" + args[0].Value;
            if (SourceLookup.FindStat(alias) is not { } stat) {
                TextureExpand.LogError($"#con requires valid condition alias: {alias}");
                return null;
            }

            var id = stat.id;
            return c => c.conditions.Any(x => !x.IsKilled && x.id == id) == expected;
        }, 1, Trigger.Condition, true);

        Register("portrait", (args, expected) => {
            if (!HasArgs(args, "portrait")) {
                return null;
            }

            var phrase = args[0].Value;
            return c => ContainsIgnoreCase(c.c_idPortrait, phrase) == expected;
        }, 1, Trigger.Zone, true);

        Register("floor", (args, expected) => {
            if (!HasArgs(args, "floor")) {
                return null;
            }

            var floorType = 0;
            if (!int.TryParse(args[0].Value, out var matType)) {
                if (SourceLookup.FindMaterial(args[0].Value) is { } material) {
                    matType = material.id;
                } else if (args.Count == 1) {
                    var name = args[0].Value;
                    return c => ContainsIgnoreCase(c.pos?.cell?.GetFloorName(), name) == expected;
                }
            }

            if (args.Count > 1 && !int.TryParse(args[1].Value, out floorType) &&
                SourceLookup.FindFloor(args[1].Value) is { } floor) {
                floorType = floor.id;
            }

            if (matType == 0) {
                return c => c.pos?.cell?._floor == floorType == expected;
            }

            if (floorType == 0) {
                return c => c.pos?.cell?._floorMat == matType == expected;
            }

            return c => (c.pos?.cell is { } cell && cell._floor == floorType && cell._floorMat == matType) == expected;
        }, 1, Trigger.Move, true);

        Register("indoor", (args, expected) => {
            if (args.Count < 1) {
                return c => c.pos?.cell?.room is not null == expected;
            }

            var phrase = args[0].Value;
            return c => c.pos?.cell?.room is { } room
                ? ContainsIgnoreCase(room.Name, phrase) == expected
                : !expected;
        }, 1, Trigger.Move, true);

        Register("outdoor", (_, expected) => c => c.pos?.cell?.room is null == expected, 1, Trigger.Move, true);

        Register("zone", (args, expected) => {
            if (!HasArgs(args, "zone")) {
                return null;
            }

            var arg = args[0].Value;
            Func<Chara, bool> judge = arg switch {
                "pc" => _ => EClass._zone.IsPCFaction == expected,
                "tent" => _ => EClass._zone is Zone_Tent == expected,
                "field" => _ => EClass._zone is Zone_Field == expected,
                "dungeon" => _ => EClass._zone is Zone_Dungeon == expected,
                "civilized" => _ => EClass._zone is Zone_Civilized == expected,
                "town" => _ => EClass._zone is Zone_Town == expected,
                _ => _ => {
                    var zone = EClass._zone;
                    return (ContainsIgnoreCase(zone.id, arg) ||
                            ContainsIgnoreCase(zone.Name, arg) ||
                            ContainsIgnoreCase(zone.GetType().Name, arg)) == expected;
                },
            };

            return judge;
        }, 1, Trigger.Zone, true);

        Register("month", (args, expected) => {
            if (!HasArgs(args, "month")) {
                return null;
            }

            var value = args[0].Value;
            switch (value) {
                case "spring":
                    return _ => EClass.world.season.isSpring == expected;
                case "summer":
                    return _ => EClass.world.season.isSummer == expected;
                case "autumn":
                    return _ => EClass.world.season.isAutumn == expected;
                case "winter":
                    return _ => EClass.world.season.isWinter == expected;
            }

            if (!int.TryParse(value, out var start)) {
                TextureExpand.LogError("#month requires int or season name");
                return null;
            }

            if (args.Count < 2 || !int.TryParse(args[1].Value, out var end)) {
                return _ => EClass.world.date.month == start == expected;
            }

            if (start <= end) {
                return _ => (start <= EClass.world.date.month && EClass.world.date.month <= end) == expected;
            }

            return _ => (EClass.world.date.month <= end || start <= EClass.world.date.month) == expected;
        }, 1, Trigger.Zone, false);

        Register("weather", (args, expected) => {
            if (!HasArgs(args, "weather")) {
                return null;
            }

            if (!Enum.TryParse<Weather.Condition>(args[0].Value, true, out var condition)) {
                TextureExpand.LogError($"#weather requires valid weather condition: {args[0].Value}");
                return null;
            }

            return _ => EClass.world.weather.CurrentCondition == condition == expected;
        }, 1, Trigger.Zone, false);

        Register("snow", (_, expected) => _ => EClass._zone.IsSnowCovered == expected, 1, Trigger.Zone, false);
        Register("battle", (_, expected) => c => c.enemy is not null == expected, 1, Trigger.Battle, false);
        Register("tail", (_, expected) => c => RuleState.TailingUids.Contains(c.uid) == expected, 1, Trigger.Tail, false);

        Register("hostility", (args, expected) => {
            if (!HasArgs(args, "hostility")) {
                return null;
            }

            if (!Enum.TryParse<Hostility>(args[0].Value, true, out var hostility)) {
                TextureExpand.LogError("#hostility requires valid hostility type");
                return null;
            }

            return c => c.hostility == hostility == expected;
        }, 1, Trigger.Hostility, false);

        Register("fur", (_, expected) => c => (c.HaveFur() && c.c_fur >= 0) == expected, 1, Trigger.Fur, false);

        Register("height", (args, expected) => {
            if (!HasInt(args, "height", out var height)) {
                return null;
            }

            return c => (c.bio?.height >= height) == expected;
        }, 1, Trigger.Zone, false);

        Register("weight", (args, expected) => {
            if (!HasInt(args, "weight", out var weight)) {
                return null;
            }

            return c => (c.bio?.weight >= weight) == expected;
        }, 1, Trigger.Zone, false);

        Register("age", (args, expected) => {
            if (!HasInt(args, "age", out var age)) {
                return null;
            }

            return c => (c.bio?.GetAge(c) >= age) == expected;
        }, 1, Trigger.Zone, false);

        Register("faith", (args, expected) => {
            if (!HasArgs(args, "faith")) {
                return null;
            }

            var id = args[0].Value;
            if (!SourceLookup.HasReligion(id)) {
                TextureExpand.LogError($"#faith requires valid faith: {id}");
                return null;
            }

            return c => c.idFaith == id == expected;
        }, 1, Trigger.Zone, false);
    }

    internal static bool Has(string? charaId, Trigger trigger)
    {
        return charaId is not null && Targets.TryGetValue(charaId, out var triggers) && (triggers & trigger) != 0;
    }

    internal static bool IsAnyTarget(string? charaId)
    {
        return charaId is not null && Targets.ContainsKey(charaId);
    }

    internal static void AddTarget(string charaId, Trigger triggers)
    {
        if (triggers == Trigger.None) {
            return;
        }

        Targets.TryGetValue(charaId, out var existing);
        Targets[charaId] = existing | triggers;
    }

    private static void Register(string keyword, JudgeFactory factory, int priority, Trigger trigger, bool allowMulti)
    {
        All.Add(new(keyword, factory, priority, trigger, allowMulti));
    }

    private static bool HasArgs(CaptureCollection args, string keyword)
    {
        if (args.Count >= 1) {
            return true;
        }

        TextureExpand.LogError($"#{keyword} requires at least 1 argument");
        return false;
    }

    private static bool HasInt(CaptureCollection args, string keyword, out int value)
    {
        value = 0;
        if (!HasArgs(args, keyword)) {
            return false;
        }

        if (int.TryParse(args[0].Value, out value)) {
            return true;
        }

        TextureExpand.LogError($"#{keyword} requires valid {keyword}: {args[0].Value}");
        return false;
    }

    private static SourceElement.Row? FindElement(CaptureCollection args, string keyword)
    {
        var element = SourceLookup.FindElement(args[0].Value);
        if (element is null) {
            TextureExpand.LogError($"#{keyword} requires valid element alias: {args[0].Value}");
        }

        return element;
    }

    private static bool ContainsIgnoreCase(string? source, string value)
    {
        return source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;
    }
}