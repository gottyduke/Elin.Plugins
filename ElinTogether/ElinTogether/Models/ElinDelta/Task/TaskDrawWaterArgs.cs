using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskDrawWaterArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    [Key(1)]
    public required RemoteCard PotOwner { get; init; }

    public static TaskDrawWaterArgs Create(TaskDrawWater task)
    {
        return new() {
            Pos = task.pos,
            PotOwner = task.pot.owner,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskDrawWater {
            pos = Pos,
            pot = PotOwner.Find()?.trait as TraitToolWaterPot,
        };
    }
}