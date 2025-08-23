using System.Collections.Generic;
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

public class CustomQuestStorageV1 : Quest
{
    [JsonProperty] public SerializableQuestData Data { get; protected set; } = new();
    [JsonProperty] public Dictionary<QuestTextType, string> Text { get; protected set; } = [];
}