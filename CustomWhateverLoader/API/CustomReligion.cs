using System.Collections.Generic;

namespace Cwl.API;

public class CustomReligion(string religionId) : Religion
{
    private static readonly Dictionary<string, CustomReligion> _cached = [];
    private bool _canJoin;
    private bool _isMinor;

    public override string id => religionId;
    public override bool IsMinorGod => _isMinor;
    public override bool CanJoin => _canJoin;

    public static IEnumerable<CustomReligion> All => _cached.Values;

    public static CustomReligion GerOrAdd(string id)
    {
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
}