using System.Collections.Generic;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateDeltaList
{
    [Key(0)]
    public List<ElinDelta> DeltaList { get; set; } = [];
}