using System.Collections.Generic;
using Emmersive.Contexts.Memory;

namespace Emmersive.API.Plugins;

internal sealed class RecentLineGuard
{
    internal const int PerSpeakerCapacity = 8;
    internal const float CrossSpeakerSeconds = 90f;
    internal const float ShortLineSeconds = 60f;

    private const int CrossSpeakerCapacity = 64;

    private readonly Dictionary<int, List<(string Text, float Time)>> _bySpeaker = [];
    private readonly List<(int Uid, string Text, float Time)> _recent = [];

    internal bool IsRepeat(int uid, string text, bool gesture, float now)
    {
        var substantive = !gesture && MemoryRelevance.IsSubstantive(text);

        if (_bySpeaker.TryGetValue(uid, out var own)) {
            foreach (var (line, time) in own) {
                if ((substantive || now - time <= ShortLineSeconds) && MemoryRelevance.IsNearDuplicate(line, text)) {
                    return true;
                }
            }
        }

        if (!substantive) {
            return false;
        }

        foreach (var (lineUid, line, time) in _recent) {
            if (lineUid != uid && now - time <= CrossSpeakerSeconds && MemoryRelevance.IsNearDuplicate(line, text)) {
                return true;
            }
        }

        return false;
    }

    internal void Add(int uid, string text, float now)
    {
        if (!_bySpeaker.TryGetValue(uid, out var own)) {
            own = [];
            _bySpeaker[uid] = own;
        }

        own.Add((text, now));
        if (own.Count > PerSpeakerCapacity) {
            own.RemoveAt(0);
        }

        _recent.RemoveAll(r => now - r.Time > CrossSpeakerSeconds);
        _recent.Add((uid, text, now));
        if (_recent.Count > CrossSpeakerCapacity) {
            _recent.RemoveAt(0);
        }
    }

    internal void Clear()
    {
        _bySpeaker.Clear();
        _recent.Clear();
    }
}