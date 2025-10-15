using System.Collections.Concurrent;
using Cwl.API.Attributes;
using Emmersive.API.Profiles;

namespace Emmersive.Helper;

public static class ProfileHelper
{
    private static readonly ConcurrentDictionary<int, CharaProfile> _profiles = [];

    [CwlPostLoad]
    internal static void ClearProfiles()
    {
        _profiles.Clear();
    }

    extension(Chara chara)
    {
        public CharaProfile Profile => _profiles.GetOrAdd(chara.uid, _ => new(chara));
        public string UnifiedId => chara.IsPC ? "player" : chara.id;
    }
}