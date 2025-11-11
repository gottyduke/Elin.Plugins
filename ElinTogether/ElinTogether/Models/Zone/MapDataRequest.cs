using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class MapDataRequest
{
    [field: AllowNull]
    public static MapDataRequest CurrentRemoteZone =>
        field ??= new() {
            ZoneFullName = "",
            ZoneUid = -1,
        };

    [Key(0)]
    public required string ZoneFullName { get; init; }

    [Key(1)]
    public required int ZoneUid { get; init; }
}