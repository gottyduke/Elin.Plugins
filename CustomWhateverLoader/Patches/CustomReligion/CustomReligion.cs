using System.Collections.Generic;

namespace Cwl.Patches.CustomReligion;

internal class CustomReligion(string religionId) : Religion
{
    private static readonly Dictionary<string, CustomReligion> _cached = [];
    private bool _canJoin;
    private bool _isMinor;

    public override string id => religionId;
    public override bool IsMinorGod => _isMinor;
    public override bool CanJoin => _canJoin;

    internal static IEnumerable<CustomReligion> All => _cached.Values;

    internal static CustomReligion GerOrAdd(string id)
    {
        _cached.TryAdd(id, new(id));
        return _cached[id];
    }

    internal CustomReligion SetMinor(bool minorGod)
    {
        _isMinor = minorGod;
        return this;
    }

    internal CustomReligion SetCanJoin(bool canJoin)
    {
        _canJoin = canJoin;
        return this;
    }
}