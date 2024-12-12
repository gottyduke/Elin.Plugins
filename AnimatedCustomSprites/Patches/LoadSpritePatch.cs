﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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
                new CodeInstruction(OpCodes.Ldarg, 4),
                Transpilers.EmitDelegate(TryLoadExtensively),
                new CodeInstruction(OpCodes.Brtrue, jmp),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
    }

    internal static bool TryLoadExtensively(bool loaded, SpriteData data, string suffix)
    {
        if (loaded || suffix == "_snow.png") {
            return true;
        }

        var baseFile = new FileInfo($"{data.path}.png");
        var baseName = Path.GetFileNameWithoutExtension(baseFile.Name);

        var altTex = baseFile.Directory!
            .GetFiles("*.png", SearchOption.TopDirectoryOnly)
            .Where(f => f.Name != baseFile.Name && f.Name.Contains("_acs_"))
            .Where(f => f.Name.StartsWith(baseName))
            .OrderBy(f => f.Name);

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
            .Select(t => Sprite.Create(t, new(0, 0, t.width, t.height),
                t.AdjustPivot(), 100f, 0u, SpriteMeshType.FullRect))
            .ToArray();
        data.sprites.Do(s => s.name = s.texture.name);
        data.frame = 1;

        return false;
    }
}