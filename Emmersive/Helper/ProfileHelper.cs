using System.Collections.Concurrent;
using Emmersive.API.Profiles;

namespace Emmersive.Helper;

public static class ProfileHelper
{
    private static readonly ConcurrentDictionary<int, CharaProfile> _profiles = [];

    [ElinPostLoad]
    internal static void ClearProfiles(GameIOContext context)
    {
        _profiles.Clear();
    }

    extension(Chara chara)
    {
        public CharaProfile Profile
        {
            get {
                var profile = _profiles.GetOrAdd(chara.uid, _ => new(chara));
                profile.Chara = chara;
                return profile;
            }
        }

        public string UnifiedId => chara.IsPC ? "player" : chara.id;
    }
}