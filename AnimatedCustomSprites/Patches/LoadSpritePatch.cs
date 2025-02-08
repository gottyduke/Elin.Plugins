using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Cwl.Helper.Unity;
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

        var baseTex = $"{data.path}.png";
        var baseName = Path.GetFileNameWithoutExtension(baseTex);

        List<Sprite> sprites = [];

        foreach (var (name, file) in SpriteReplacer.dictModItems) {
            if (!name.StartsWith($"{baseName}_acs_")) {
                continue;
            }

            try {
                var tex = IO.LoadPNG($"{file}.png");
                if (tex == null) {
                    continue;
                }

                var sprite = Sprite.Create(tex, new(0f, 0f, tex.width, tex.height), new(0.5f, 0.5f * (128f / tex.height)), 100f,
                    0u, SpriteMeshType.FullRect);
                if (sprite == null) {
                    continue;
                }

                var index = name.Split('_')[^1];
                var frameName = name[..^index.Length];

                if (name.Contains('-')) {
                    var indexes = index.Split('-');
                    int.TryParse(indexes[0], out var begin);
                    int.TryParse(indexes[1], out var end);

                    sprites.AddRange(SliceSheet(sprite, begin, end, frameName));
                } else {
                    int.TryParse(index, out var frame);
                    sprite.name = tex.name =  $"{frameName}{frame:D4}";

                    sprites.Add(sprite);
                }
            } catch (Exception ex) {
                AcsMod.Warn($"failed to load acs sprite {file}\n{ex.Message}");
                // noexcept
            }
        }

        if (sprites.Count > 0) {
            sprites.Insert(0, baseTex.LoadSprite(name: baseName)!);
        } else {
            return true;
        }

        data.sprites = sprites.ToArray();
        data.tex = data.sprites[0].texture;

        return false;
    }

    private static IEnumerable<Sprite> SliceSheet(Sprite sheet, int begin, int end, string baseName)
    {
        var frames = end - begin + 1;
        var width = sheet.rect.width / frames;
        var height = sheet.rect.height;

        for (var i = 0; i < frames; ++i) {
            Rect rect = new(i * width, 0f, width, height);
            var sprite = Sprite.Create(sheet.texture, rect, new(0.5f, 0.5f * (128f / height)), 100f, 0u, SpriteMeshType.FullRect);
            sprite.name = $"{baseName}{i:D4}";
            yield return sprite;
        }
    }
}