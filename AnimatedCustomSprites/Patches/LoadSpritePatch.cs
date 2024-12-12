using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using ACS.Helper;
using HarmonyLib;
using UnityEngine;

namespace ACS.Patches;

[HarmonyPatch]
internal class LoadSpritePatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SpriteData), nameof(SpriteData.Load))]
    internal static IEnumerable<CodeInstruction> OnLoadSpritesIl(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_2),
                new CodeMatch(OpCodes.Ldind_Ref),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Brfalse))
            .CreateLabel(out var jmp)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryLoadExtensively),
                new CodeInstruction(OpCodes.Brtrue, jmp),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
    }

    internal static bool TryLoadExtensively(bool loaded, SpriteData data)
    {
        if (loaded) {
            return true;
        }

        var baseFile = new FileInfo($"{data.path}.png");
        var baseName = Path.GetFileNameWithoutExtension(baseFile.Name);

        var altTex = baseFile.Directory!
            .GetFiles("*.png", SearchOption.TopDirectoryOnly)
            .Where(f => f.Name != baseFile.Name && f.Name.Contains("_acs_"))
            .Where(f => f.Name.StartsWith(baseName))
            .OrderBy(f => f.Name)
            .ToArray();

        if (altTex.Length == 0) {
            return true;
        }

        var baseTex = IO.LoadPNG(baseFile.FullName);
        baseTex.name = baseName;

        List<Texture2D> allTex = [baseTex];
        foreach (var alt in altTex) {
            var tex = IO.LoadPNG(alt.FullName);
            tex.name = alt.Name;
            allTex.Add(tex);
        }

        data.tex = allTex[0];
        data.sprites = allTex
            .SelectMany(t => {
                var index = t.name.Split("_")[^1];
                if (!index.Contains("-")) {
                    return [
                        Sprite.Create(t, new(0, 0, t.width, t.height), new(0.5f, t.AdjustPivot()), 100f, 0u,
                            SpriteMeshType.FullRect),
                    ];
                }

                var regex = new Regex(@"\d+");
                var matches = regex.Matches(index);
                if (!int.TryParse(matches[0].Value, out var begin) ||
                    !int.TryParse(matches[1].Value, out var end)) {
                    AcsMod.Warn($"failed to create sequential frames from sheet: {t.name}");
                    return [];
                }

                var count = end - begin + 1;
                var width = t.width / count;
                var sprites = new Sprite[count];
                for (var i = 0; i < count; ++i) {
                    sprites[i] = Sprite.Create(t, new(i * width, 0f, width, t.height),
                        new(0.5f, t.AdjustPivot(width)), 100f, 0u, SpriteMeshType.FullRect);
                }

                return sprites;
            })
            .ToArray();
        data.sprites.Do(s => s.name = s.texture.name);

        return false;
    }
}