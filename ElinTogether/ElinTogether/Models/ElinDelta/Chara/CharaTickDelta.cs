using System.Collections.Generic;
using System.Diagnostics;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaTickDelta : ElinDeltaBase
{
    private static readonly Dictionary<int, long> _lastTicked = [];
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    [Key(0)]
    public required RemoteCard Owner { get; init; }

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        // we are host, relay the client tick to other players
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        // do not remote tick a client
        if (chara.IsPC) {
            return;
        }

        var thisTick = _sw.ElapsedMilliseconds;
        var lastTick = _lastTicked.GetValueOrDefault(chara.uid);
        var elapsed = thisTick - lastTick;

        // each slow tick is 240ms, each fast tick is 120ms
        // we buffer it and avoid duplicate ticks
        if (elapsed <= 235) {
            return;
        }

        _lastTicked[chara.uid] = thisTick;

        // do a remote tick
        chara.Stub_Tick();
    }
}