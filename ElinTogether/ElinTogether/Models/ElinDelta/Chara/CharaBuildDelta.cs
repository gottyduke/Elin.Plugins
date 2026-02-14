using ElinTogether.Elements;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaBuildDelta : ElinDeltaBase
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

    public override void Apply(ElinNetBase net)
    {
        if (Owner.Find() is not Chara { IsPC: false } chara || Held.Find() is not Card held) {
            return;
        }

        // relay to clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        if (chara.ai is not GoalRemote) {
            return;
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

        CharaBuildCompleteEvent.OnProgressComplete_Modified(taskBuild);
    }
}