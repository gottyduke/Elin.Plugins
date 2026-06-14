using ElinTogether.Net;
using MessagePack;
using UnityEngine;

namespace ElinTogether.Models;

[MessagePackObject]
public class MsgSayDelta : ElinDelta
{
    [Key(1)]
    public required string Text { get; init; }

    [Key(2)]
    public required float R { get; init; }

    [Key(3)]
    public required float G { get; init; }

    [Key(4)]
    public required float B { get; init; }

    [Key(5)]
    public required float A { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (net is ElinNetHost) {
            return;
        }

        Msg.SetColor(new Color {
            r = R,
            g = G,
            b = B,
            a = A,
        });
        Msg.Say(Text);
    }
}