using System.Collections.Generic;
using Cwl.Helper.Extensions;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class ZoneContext(Zone zone) : ContextProviderBase
{
    public override string Name => "zone_data";

    protected override IDictionary<string, object>? BuildInternal()
    {
        Dictionary<string, object> data = new() {
            ["name"] = zone.NameWithDangerLevel,
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
                data["allow_crime"] = zone.AllowCriminal;
                data["has_law"] = zone.HasLaw;
                data["is_town"] = zone.IsTown;
                data["player_influence"] = zone.influence;

                if (zone.IsFestival) {
                    data["is_festival"] = true;
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