using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class MapDataRequest
{
    [field: AllowNull]
    public static MapDataRequest CurrentRemoteZone => field ??= new();

    [Key(0)]
    public string ZoneFullName { get; set; } = "";

    [Key(1)]
    public int ZoneUid { get; set; } = -1;
}