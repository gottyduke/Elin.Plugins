using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.LangMod;
using MethodTimer;
using Exception = System.Exception;

namespace Cwl.API.Custom;

public class CustomZone : Zone
{
    internal static readonly Dictionary<string, SourceZone.Row> Managed = [];

    public static IReadOnlyCollection<SourceZone.Row> All => Managed.Values;

    [Time]
    public static void AddZone(SourceZone.Row r)
    {
        try {
            Managed[r.id] = r;
        } catch (Exception ex) {
            CwlMod.ErrorWithPopup<CustomZone>("cwl_error_qualify_type".Loc(nameof(Zone), r.id, r.type), ex);
            // noexcept
        }
    }

    [CwlSceneInitEvent(Scene.Mode.StartGame, true, order: CwlSceneEventOrder.ZoneImporter)]
    public static void AddDelayedZone()
    {
        foreach (var row in All) {
            if (game.spatials.Find(row.id) is not null) {
                continue;
            }

            if (row.tag.Contains("addMap")) {
                SpatialGen.Create(row.id, world.region, true);
            }
        }
    }
}