using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cwl.Helper.Extensions;
using ElinTogether.Helper;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ZoneDataResponse
{
    [Key(0)]
    public required string ZoneFullName { get; init; }

    [Key(1)]
    public required int ZoneUid { get; init; }

    [Key(2)]
    public required LZ4Bytes Zone { get; init; }

    // TODO use a map surrogate for more efficient transporting
    [Key(3)]
    public required Dictionary<string, LZ4Bytes> Map { get; init; }

    [return: NotNullIfNotNull("zone")]
    public static implicit operator ZoneDataResponse?(Zone? zone)
    {
        return Create(zone);
    }

    [return: NotNullIfNotNull("zone")]
    public static ZoneDataResponse? Create(Zone? zone)
    {
        if (zone is null) {
            return null;
        }

        zone.map?.Save(zone.pathSave);

        return new() {
            ZoneFullName = zone.ZoneFullName,
            ZoneUid = zone.uid,
            Zone = LZ4Bytes.Create(zone),
            Map = Directory
                .GetFiles(zone.pathSave, "*.*", SearchOption.TopDirectoryOnly)
                .ToDictionary(Path.GetFileNameWithoutExtension, LZ4Bytes.CreateFromFile),
        };
    }

    public void WriteToTemp()
    {
        var basePath = ResourceFetch.GetEmpSavePath();

        foreach (var (id, asset) in Map) {
            var path = Path.Combine(basePath, ZoneUid.ToString(), id);
            asset.DecompressToFile(path);
        }

        EmpLog.Debug("Saved map {ZoneUid} to temp folder",
            ZoneUid);
    }
}