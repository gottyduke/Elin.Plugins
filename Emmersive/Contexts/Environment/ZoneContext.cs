using System.Collections.Generic;
using Cwl.Helper.Extensions;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class ZoneContext(Zone zone) : ContextProviderBase
{
    public override string Name => "zone_data";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var world = EClass.world;
        var season = world.date.month switch {
            >= 3 and <= 5 => "Spring",
            >= 6 and <= 8 => "Summer",
            >= 9 and <= 11 => "Autumn",
            12 or >= 1 and <= 2 => "Winter",
            _ => "Unknown",
        };

        var data = new Dictionary<string, object> {
            ["name"] = zone.NameWithDangerLevel,
            ["date"] =
                $"{world.date.GetText(Date.TextFormat.Widget)}, {world.date.NameTime}, {season}, {world.weather.GetName()}",
        };

        if (zone.IsRegion) {
            //data["type"] = "World Map of North Tyris";
            return null;
        }

        switch (zone) {
            case Zone_Dungeon or Zone_RandomDungeon:
                data["type"] = "Dungeon";
                break;
            case Zone_Civilized:
                data["type"] = "Town";
                if (zone.AllowCriminal) {
                    data["crime"] = "Allowed";
                }

                data["influence"] = zone.influence;

                if (zone.IsFestival) {
                    data["festival"] = true;
                }

                break;
        }

        if (zone.IsUnderwater) {
            data["underwater"] = true;
        }

        if (EClass.pc.Cell.room is { } room) {
            data["in_room"] = room.Name;
        }

        var background = ResourceFetch.GetActiveResource($"Emmersive/Zones/{zone.ZoneFullName}.txt")
            .IsEmpty(ResourceFetch.GetActiveResource($"Emmersive/Zones/Zone_{zone.id}.txt"));
        if (!background.IsEmpty()) {
            data["background"] = background;
        }

        return data;
    }
}