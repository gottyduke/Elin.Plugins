using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.Components;

internal partial class CwlConsole
{
    [ConsoleCommand("dump_race_food_data")]
    internal static string DumpMeatData()
    {
        const string header = "id,name,STR,END,DEX,PER,LER,WIL,MAG,CHA";

        List<(string food, StringBuilderPool sb)> exporters = [
            new("_meat", StringBuilderPool.Get()),
            new("dattamono", StringBuilderPool.Get()),
            new("meat_marble", StringBuilderPool.Get()),
            new("_egg", StringBuilderPool.Get()),
        ];

        foreach (var exporter in exporters) {
            exporter.sb.AppendLine(header);
        }

        var checkedRaces = new HashSet<SourceRace.Row>();
        var dummy = CharaGen.Create("chara");

        foreach (var r in EClass.sources.races.rows.Where(checkedRaces.Add)) {
            dummy.ChangeRace(r.id);

            foreach (var exporter in exporters) {
                var food = ThingGen.Create(exporter.food).MakeFoodFrom(dummy);
                exporter.sb.AppendLine($"{r.id},{r.GetName()}," +
                                       $"{Value(food.STR)},{Value(food.END)},{Value(food.DEX)},{Value(food.PER)}," +
                                       $"{Value(food.LER)},{Value(food.WIL)},{Value(food.MAG)},{Value(food.CHA)}");
            }
            continue;

            string Value(int value)
            {
                return value switch {
                    > 0 => $"{value / 10 + 1} ({value})",
                    < 0 => $"{value / 10 - 1} ({value})",
                    _ => "",
                };
            }
        }

        var dump = $"{CorePath.rootExe}/Food/";
        Directory.CreateDirectory(dump);

        foreach (var exporter in exporters) {
            File.WriteAllText(Path.Combine(dump, $"{exporter.food}.csv"), exporter.sb.ToString());
            exporter.sb.Dispose();
        }

        return $"output has been dumped to {dump.NormalizePath()}";
    }
}