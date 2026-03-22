using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using ElinTogether.Net;

namespace ElinTogether.Models;

public static class CardCache
{
    private static readonly Dictionary<int, WeakReference<Card>> _cards = [];
    // prevent temporary item cache invalidation
    private static readonly List<Card> _keepalive = [];

    private static bool IsHost { get; set; }

    private static bool IsClient => !IsHost;

    internal static void Add(Card card)
    {
        var stored = Find(card.uid);
        if (stored is null) {
            Set(card);
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

        Set(card);

        stored.uid++;
        Add(stored);
        EClass.game.cards.uidNext = Math.Max(stored.uid, EClass.game.cards.uidNext);
    }

    internal static void Set(Card card)
    {
        _cards[card.uid] = new(card);
        if (IsClient && card.parent is null) {
            KeepAlive(card);
        }
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

        foreach (var card in map.Cards) {
            Add(card);
            CacheContainer(card.things);
        }
    }

    internal static void CacheContainer(ThingContainer container)
    {
        foreach (var thing in container.Flatten()) {
            Add(thing);
        }
    }

    internal static void KeepAlive(Card card)
    {
        _keepalive.Add(card);
    }

    [CwlPreLoad]
    [CwlSceneInitEvent(Scene.Mode.Title, preInit: true)]
    private static void ClearCachedRefs()
    {
        _cards.Clear();
        _keepalive.Clear();
    }

    public static void Update()
    {
        IsHost = NetSession.Instance.IsHost;
        _keepalive.RemoveAll(card => card.parent is not null);
        foreach (var (uid, reference) in _cards.ToArray()) {
            if (!reference.TryGetTarget(out _)) {
                _cards.Remove(uid);
            }
        }
    }

    extension(Card card)
    {
        internal bool IsKeptAlive => _keepalive.Contains(card);
    }
}