using System.Collections.Generic;
using Cwl.API.Custom;
using Cwl.Helper.FileUtil;
using Newtonsoft.Json;

namespace Cwl.API;

public sealed record SerializableQuestData : SerializableQuestDataV1;

// ReSharper disable all
public record SerializableQuestDataV1
{
    public bool CanAbandon = false;

    [JsonConverter(typeof(RangedIntConverter), 1, 7)]
    public int Difficulty = 1;
    public List<string> DramaTriggers = [];
    public Dictionary<string, string> FailConditions = [];
    public bool ForbidTeleport = false;
    public int RangeDeadLine = 0;
    public bool Replayable = false;
    public bool RequireClientInSameZone = false;
    public Dictionary<string, string> StartConditions = [];

    public Dictionary<QuestTextType, string> Texts = [];
    public bool UseInstanceZone = false;
}