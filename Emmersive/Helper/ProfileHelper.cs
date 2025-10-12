using System.Collections.Concurrent;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Emmersive.API.Profiles;

namespace Emmersive.Helper;

public static class ProfileHelper
{
    private static readonly ConcurrentDictionary<int, CharaProfile> _profiles = [];

    [CwlPostLoad]
    internal static void ClearProfiles(GameIOProcessor.GameIOContext context)
    {
        _profiles.Clear();
    }

    extension(Chara chara)
    {
        public CharaProfile Profile => _profiles.GetOrAdd(chara.uid, uid => new(uid));
    }
}