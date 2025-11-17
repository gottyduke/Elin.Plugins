using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class AIPlayMusicArgs : TaskArgsBase
{
    [Key(0)]
    public required RemoteCard Tool { get; init; }

    public static AIPlayMusicArgs Create(AI_PlayMusic task)
    {
        return new() {
            Tool = task.tool,
        };
    }

    public override AIAct CreateSubAct()
    {
        return new AI_PlayMusic {
            id = ABILITY.AI_PlayMusic,
            tool = Tool,
        };
    }
}