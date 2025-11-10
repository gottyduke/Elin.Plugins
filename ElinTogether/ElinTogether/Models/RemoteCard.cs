using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class RemoteCard
{
    public enum CardType : byte
    {
        Thing,
        Chara,
    }

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
            Parent = card.parentCard is null ? null : Create(card.parentCard),
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
        var map = EClass.game?.activeZone?.map;
        Card? card = Type switch {
            CardType.Thing => map?.FindThing(Uid),
            CardType.Chara => map?.FindChara(Uid) ??
                              EClass.game?.cards.globalCharas.GetValueOrDefault(Uid),
            _ => null,
        };

        card ??= CardGenEvent.TryPop(Uid);

        if (Parent is not null) {
            card = Parent.Find()?.things.Find(Uid);
        }

        return card;
    }
}