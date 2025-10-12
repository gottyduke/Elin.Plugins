using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Emmersive.Contexts;

public class ZoneContext(Zone zone) : FileContextBase<ZoneContext.ZoneBackground>
{
    public override string Name => "zone_data";

    [field: AllowNull]
    public static ZoneContext Default => field ??= new(null!);

    protected override IDictionary<string, object> BuildCore()
    {
        Dictionary<string, object> data = new() {
            ["name"] = zone.NameWithDangerLevel,
        };

        if (zone.IsRegion) {
            data["type"] = "World Map of North Tyris";
        } else {
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
        }

        if (zone.IsUnderwater) {
            data["underwater"] = true;
        }

        if (EClass.pc.Cell.room is { } room) {
            data["in_room"] = room.Name;
        }

        return data;
    }

    protected override ZoneBackground LoadFromFile(FileInfo file)
    {
        var path = file.FullName;
        var id = Path.GetFileNameWithoutExtension(path);
        return new(id, File.ReadAllText(path), file);
    }

    public static void Init()
    {
        Lookup = Default.LoadAllContexts("Emmersive/Zones").ToLookup(ctx => ctx.ZoneId);
    }

    public static void Clear()
    {
        Lookup = null!;
        Overrides.Clear();
    }

    public record ZoneBackground(string ZoneId, string Prompt, FileInfo Provider);
}