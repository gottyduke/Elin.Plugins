using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TaskPourWaterArgs : TaskArgsBase
{
    [Key(0)]
    public required Position Pos { get; init; }

    [Key(1)]
    public required RemoteCard PotOwner { get; init; }

    public static TaskPourWaterArgs Create(TaskPourWater task)
    {
        return new() {
            Pos = task.pos,
            PotOwner = task.pot.owner,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new TaskPourWater {
            pos = Pos,
            pot = PotOwner.Find()?.trait as TraitToolWaterPot,
        };
    }
}