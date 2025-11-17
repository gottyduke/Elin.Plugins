using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cwl.API.Attributes;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Card surrogate
/// </summary>
[MessagePackObject]
public class RemoteCard
{
    public enum CardType : byte
    {
        Thing,
        Chara,
    }

    private static readonly Dictionary<int, WeakReference<Card>> _cached = [];

    [Key(0)]
    public required int Uid { get; init; }

    [Key(1)]
    public required CardType Type { get; init; }

    [Key(2)]
    public RemoteCard? Parent { get; init; }

    [Key(3)]
    public LZ4Bytes? Data { get; set; }

    [return: NotNullIfNotNull("card")]
    public static RemoteCard? Create(Card? card, bool withData = false)
    {
        if (card is null) {
            return null;
        }

        return new() {
            Uid = card.uid,
            Type = card is Thing ? CardType.Thing : CardType.Chara,
            // do not compress parent
            Parent = card.parentCard,
            Data = withData ? LZ4Bytes.Create(card) : null,
        };
    }

    [return: NotNullIfNotNull("card")]
    public static implicit operator RemoteCard?(Card? card)
    {
        return Create(card);
    }

    public static implicit operator Chara?(RemoteCard? remote)
    {
        return remote?.Find() as Chara;
    }

    public static implicit operator Thing?(RemoteCard? remote)
    {
        return remote?.Find() as Thing;
    }

    public static implicit operator Card?(RemoteCard? remote)
    {
        return remote?.Find();
    }

    public Card? Find()
    {
        if (_cached.TryGetValue(Uid, out var reference) && reference.TryGetTarget(out var card)) {
            return card;
        }

        var map = EClass.game?.activeZone?.map;
        card = Type switch {
            CardType.Thing => map?.FindThing(Uid),
            CardType.Chara => Uid == EClass.player.uidChara
                ? EClass.pc
                : map?.FindChara(Uid) ??
                  EClass.game?.cards.globalCharas.GetValueOrDefault(Uid),
            _ => null,
        };

        card ??= CardGenEvent.TryPop(Uid);
        card ??= Type switch {
            CardType.Thing => Data?.Decompress<Thing>(),
            CardType.Chara => Data?.Decompress<Chara>(),
            _ => null,
        };

        if (card is null && Parent is not null) {
            card = Parent.Find()?.things.Find(Uid);
        }

        _cached[Uid] = new(card, false);

        return card;
    }

    public override string ToString()
    {
        return $"{Find()}";
    }

    [CwlPreLoad]
    [CwlSceneInitEvent(Scene.Mode.Title, preInit: true)]
    private static void ClearCachedRefs()
    {
        _cached.Clear();
    }
}