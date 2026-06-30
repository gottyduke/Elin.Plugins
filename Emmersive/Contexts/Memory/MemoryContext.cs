using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;

namespace Emmersive.Contexts.Memory;

public sealed class MemoryContext : ContextProviderBase
{
    public override string Name => "npc_memories";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var nearbyCharas = PointScan.LastNearby.ToList();
        nearbyCharas.Add(EClass.pc);
        if (nearbyCharas is not { Count: > 0 }) {
            return null;
        }

        var result = new Dictionary<string, object>();
        var hasAny = false;

        foreach (var chara in nearbyCharas) {
            var store = MemoryManager.Instance.Get(chara.uid);
            if (store is not { IsEmpty: false }) {
                continue;
            }

            var memory = new Dictionary<string, object>();
            var stm = store.GetRecentStm();
            if (stm.Count > 0) {
                memory["recent_talks"] = stm.Select(e => e.ToString()).ToList();
            }

            var ltm = store.GetTopLtm();
            if (ltm.Count > 0) {
                memory["known_facts"] = ltm.Select(f => f.ToString()).ToList();
            }

            if (memory.Count > 0) {
                result[chara.NameSimple] = memory;
                hasAny = true;
            }
        }

        return hasAny ? result : null;
    }
}