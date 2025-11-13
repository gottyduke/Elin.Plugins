using System.Collections.Generic;
using ElinTogether.Models.ElinDelta;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateDeltaList
{
    [Key(0)]
    public List<ElinDeltaBase> DeltaList { get; set; } = [];
}