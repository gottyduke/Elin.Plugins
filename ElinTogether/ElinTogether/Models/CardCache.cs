using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;

namespace ElinTogether.Models;

public static class CardCache
{
    private static readonly Dictionary<int, WeakReference<Card>> _cards = [];
    // prevent temporary item cache invalidation
    private static readonly List<Card> _keepalive = [];

    internal static void Add(Card card)
    {
        var stored = Find(card.uid);
        if (stored is null) {
            _cards[card.uid] = new(card);
            return;
        }

        if (stored == card) {
            return;
        }

        // reallocate uid
        if (stored.IsGlobal || !card.IsGlobal) {
            card.uid++;
            Add(card);
            EClass.game.cards.uidNext = Math.Max(card.uid, EClass.game.cards.uidNext);
            return;
        }

        _cards[card.uid] = new(card);

        stored.uid++;
        Add(stored);
        EClass.game.cards.uidNext = Math.Max(stored.uid, EClass.game.cards.uidNext);
    }

    internal static void Set(Card card)
    {
        _cards[card.uid] = new(card);
    }

    internal static bool Contains(Card? card)
    {
        return card is not null && Find(card.uid) == card;
    }

    internal static Card? Find(int uid)
    {
        if (_cards.TryGetValue(uid, out var reference)) {
            reference.TryGetTarget(out var card);
            return card;
        }

        return null;
    }

    internal static void CacheCurrentZone()
    {
        if (EClass.game?.activeZone?.map is not { } map) {
            return;
        }

        Clean();
        foreach (var card in map.Cards) {
            Add(card);
            foreach (var thing in card.things.Flatten()) {
                Add(thing);
            }
        }
    }

    internal static void KeepAlive(Card card)
    {
        _keepalive.Add(card);
    }

    internal static void Clean()
    {
        // clean invalid weak references
        _keepalive.Clear();
        foreach (var (uid, reference) in _cards.ToArray()) {
            if (!reference.TryGetTarget(out _)) {
                _cards.Remove(uid);
            }
        }
    }

    [CwlPreLoad]
    [CwlSceneInitEvent(Scene.Mode.Title, preInit: true)]
    private static void ClearCachedRefs()
    {
        _cards.Clear();
        _keepalive.Clear();
    }

    extension(ThingContainer things)
    {
        internal IEnumerable<Thing> Flatten()
        {
            foreach (var t1 in things) {
                if (t1.things.Count == 0) {
                    yield return t1;
                    continue;
                }

                foreach (var t2 in t1.things.Flatten()) {
                    yield return t2;
                }
            }
        }
    }
}