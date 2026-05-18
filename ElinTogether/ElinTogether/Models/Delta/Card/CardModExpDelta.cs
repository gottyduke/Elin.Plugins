using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CardModExpDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Chara { get; init; }

    [Key(1)]
    public required int Ele { get; init; }

    [Key(2)]
    public required int Base { get; init; }

    [Key(3)]
    public required int Exp { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Chara.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        var element = chara.elements.GetElement(Ele);
        while (Base < element.vBase) {
            chara.elements.ModExp(Ele, -element.ExpToNext);
        }

        while (Base > element.vBase) {
            chara.elements.ModExp(Ele, element.ExpToNext);
        }

        chara.elements.ModExp(Ele, Exp - element.vExp);
    }
}