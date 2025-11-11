using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.Extensions;
using ElinTogether.Helper;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class MapDataResponse
{
    [Key(0)]
    public required string ZoneFullName { get; init; }

    [Key(1)]
    public required int ZoneUid { get; init; }

    [Key(2)]
    public required Dictionary<string, LZ4Bytes> MapAssets { get; init; }

    public void WriteToTemp()
    {
        var basePath = ResourceFetch.GetEmpSavePath();

        foreach (var (id, asset) in MapAssets) {
            var path = Path.Combine(basePath, ZoneUid.ToString(), id);
            asset.DecompressToFile(path);
        }

        EmpLog.Debug("Saved map {ZoneUid} to temp folder",
            ZoneUid);
    }

    public static MapDataResponse Create(Zone zone, bool saveImmediate = false)
    {
        if (saveImmediate || !zone.isMapSaved) {
            zone.map?.Save(zone.pathSave);
        }

        var response = new MapDataResponse {
            ZoneFullName = zone.ZoneFullName,
            ZoneUid = zone.uid,
            MapAssets = Directory
                .GetFiles(zone.pathSave, "*.*", SearchOption.TopDirectoryOnly)
                .ToDictionary(Path.GetFileNameWithoutExtension, LZ4Bytes.CreateFromFile),
        };

        return response;
    }
}