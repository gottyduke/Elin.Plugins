using System;
using System.Collections.Generic;

namespace Cwl.Helper.Extensions;

public static class CardExt
{
    extension(Card owner)
    {
        public CardDir CardDir => (CardDir)owner.dir;

        public int GetFlagValue(string flag)
        {
            var key = flag.GetHashCode();
            if (owner.mapInt.TryGetValue(key, out var value)) {
                return value;
            }

            if (EClass.core?.game?.player?.chara == owner) {
                value = EClass.player.dialogFlags.GetValueOrDefault(flag, 0);
            }

            return value;
        }

        public void SetFlagValue(string flag, int value = 1)
        {
            var key = flag.GetHashCode();
            owner.mapInt[key] = value;

            if (EClass.core?.game?.player?.chara == owner) {
                EClass.player.dialogFlags[flag] = value;
            }
        }

        public IEnumerable<Thing> FindAllThings(Func<Thing, bool> predicate)
        {
            if (owner.things is null) {
                yield break;
            }

            foreach (var thing in owner.things) {
                if (predicate(thing)) {
                    yield return thing;
                }

                if (!thing.CanSearchContents) {
                    continue;
                }

                foreach (var subThing in thing.FindAllThings(predicate)) {
                    yield return subThing;
                }
            }
        }

        public IEnumerable<Thing> FindAllThings(string id)
        {
            return owner.FindAllThings(t => string.Equals(t.id, id, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}