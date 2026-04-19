using System;
using System.Collections.Generic;
using System.Linq;
using ElinTogether.Helper;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CardGenDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Card.Data is null) {
            return;
        }

        var maxId = Math.Max(Math.Abs(Card.Uid), game.cards.uidNext);
        Card card = Card.Type == RemoteCard.CardType.Thing
            ? Card.Data.Decompress<Thing>()
            : Card.Data.Decompress<Chara>();

        maxId = card.things
            .Flatten()
            .Select(thing => thing.uid)
            .Prepend(maxId)
            .Max();

        game.cards.uidNext = maxId;

        CardCache.Add(card);
        CardCache.CacheContainer(card.things);
    }

    internal static void Refresh(List<ElinDelta> deltaList)
    {
        var alreadySent = new List<int>();
        deltaList.RemoveAll(delta => {
            if (delta is not CardGenDelta cardGenDelta) {
                return false;
            }

            var card = cardGenDelta.Card.Find();
            if (card is null || card.isDestroyed) {
                return true;
            }

            card.things.Flatten().ForEach(thing => {
                alreadySent.Add(thing.uid);
            });

            return false;
        });

        deltaList.RemoveAll(delta => {
            if (delta is not CardGenDelta cardGenDelta) {
                return false;
            }

            if (alreadySent.Contains(cardGenDelta.Card.Uid)) {
                return true;
            }

            var card = cardGenDelta.Card.Find()!;
            if (card.parent is null && card.things.Count == 0 && !card.IsKeptAlive) {
                return true;
            }

            cardGenDelta.Card.Data = LZ4Bytes.Create(card);
            return false;
        });
    }
}