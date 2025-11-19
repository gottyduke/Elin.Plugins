using Newtonsoft.Json;

namespace Cwl.API.Custom;

public enum QuestTextType
{
    Title,
    Tracker,
    Detail,
    DetailFull,
    Progress,
    Reward,
    TalkProgress,
    TalkComplete,
}

public class CustomQuestStorage : CustomQuestStorageV1;

public class CustomQuestStorageV1 : QuestDialog
{
    [JsonProperty]
    public SerializableQuestData Data { get; protected set; } = new();
}