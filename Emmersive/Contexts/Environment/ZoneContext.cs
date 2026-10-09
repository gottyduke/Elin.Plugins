using System.Collections.Generic;
using Emmersive.API.Services;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class ZoneContext(Zone zone) : ContextProviderBase
{
    public override string Name => "zone_data";

    protected override IDictionary<string, object>? BuildInternal()
    {
        var world = EClass.world;
        var data = new Dictionary<string, object> {
            ["name"] = zone.NameWithDangerLevel,
            ["date"] =
                $"{world.date.GetText(Date.TextFormat.Widget)}, {world.date.NameTime}, {world.date.NameSeason}, {world.weather.GetName()}",
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
            .OrIfEmpty(ResourceFetch.GetActiveResource($"Emmersive/Zones/Zone_{zone.id}.txt"));
        if (!background.IsEmptyOrNull) {
            data["background"] = background;
        }

        return data;
    }
}