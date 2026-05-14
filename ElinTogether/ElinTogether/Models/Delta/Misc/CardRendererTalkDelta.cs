using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CardRendererTalkDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Card { get; init; }

    [Key(1)]
    public required string Text { get; init; }

    [Key(2)]
    public required float Duration { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (net is ElinNetHost) {
            return;
        }

        if (Card.Find() is not { } card) {
            return;
        }

        card.renderer.Say(Text, duration: Duration);
    }
}