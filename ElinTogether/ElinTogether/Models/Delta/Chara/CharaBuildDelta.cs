using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaBuildDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Held { get; init; }

    [Key(1)]
    public required RemoteCard Owner { get; init; }

    [Key(2)]
    public required Position Pos { get; init; }

    [Key(3)]
    public required int Dir { get; init; }

    [Key(4)]
    public required int Altitude { get; init; }

    [Key(5)]
    public required int BridgeHeight { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara chara || Held.Find() is not Card held) {
            return;
        }

        // relay to clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        var taskBuild = new TaskBuild {
            owner = chara,
            recipe = held.trait.GetRecipe(),
            held = held,
            pos = Pos,
            dir = Dir,
            altitude = Altitude,
            bridgeHeight = BridgeHeight,
        };

        taskBuild.recipe._dir = Dir;
        taskBuild.OnProgressComplete();
    }
}