using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.FileUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cwl.API;

public sealed record SerializableQuestData : SerializableQuestDataV1;

// ReSharper disable all
public record SerializableQuestDataV1
{
    public Dictionary<string, string> StartConditions = [];
    public Dictionary<string, string> FailConditions = [];
    public Dictionary<string, string> DramaConditions = [];
    public bool Replayable = false;
    public bool CanAbandon = false;
    public int RangeDeadLine = 0;
    public bool RequireClientInSameZone = false;
    public bool UseInstanceZone = false;
    public bool ForbidTeleport = false;
    [JsonConverter(typeof(RangedIntConverter), 1, 7)]
    public int Difficulty = 1;
}