using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using VSS.Helper;
using Object = UnityEngine.Object;
using Texture2D = UnityEngine.Texture2D;

namespace VSS.Patches;

[HarmonyPatch]
internal class BuildSpritesPatch
{
    private static readonly int _color = Shader.PropertyToID("_Color");

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PCC), nameof(PCC.Build), typeof(PCCState), typeof(bool))]
    internal static IEnumerable<CodeInstruction> OnBuildSprites(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var label = generator.DefineLabel();
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(
                    typeof(PCC), nameof(PCC.pccm))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(
                    typeof(PCCManager), nameof(PCCManager.pixelize))),
                new CodeMatch(OpCodes.Brtrue))
            .AddLabels([label])
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(RebuildSprites),
                new CodeInstruction(OpCodes.Brfalse, label),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
    }

    private static bool RebuildSprites(PCC pcc)
    {
        VssMod.Log("rebuilding sprites...");

        var pccm = PCC.pccm;
        var body = pcc.layerList.list[pcc.layerList.indexBody].tex as Texture2D;

        var maxWidth = body!.width;
        var maxHeight = body.height;
        // enforce 2:3 ratio for body tex
        if (maxHeight / maxWidth != 48 / 32) {
            VssMod.Log($"body tex is not 2:3 ratio: {maxWidth}x{maxHeight}");
            return false;
        }

        try {
            // downscale body tex
            const int bodyWidth = 32 * 4;
            const int bodyHeight = 48 * 4;
            if (maxWidth > bodyWidth && maxHeight > bodyHeight) {
                VssMod.Log(
                    $"body tex is larger than {bodyWidth}x{bodyHeight}, downscaling from {maxWidth}x{maxHeight}");

                var bodyRt = new RenderTexture(bodyWidth, bodyHeight, 0, RenderTextureFormat.ARGB32);

                RenderTexture.active = bodyRt;
                GL.Clear(true, true, Color.clear);

                pccm.mat.SetColor(_color, pcc.layerList.list[pcc.layerList.indexBody].color);
                Graphics.Blit(body, bodyRt, pccm.mat);

                body = new(bodyWidth, bodyHeight, TextureFormat.ARGB32, false);
                Graphics.CopyTexture(bodyRt, body);

                maxWidth = bodyWidth;
                maxHeight = bodyHeight;
            }

            // iterate all layers to get max
            for (var i = pcc.layerList.list.Count - 1; i >= 0; --i) {
                var tex = pcc.layerList.list[i].tex;
                if (i == pcc.layerList.indexBody) {
                    tex = body;
                }

                maxWidth = Math.Max(tex.width, maxWidth);
                maxHeight = Math.Max(tex.height, maxHeight);
            }

            VssMod.Log($"max tex {maxWidth}x{maxHeight}");

            var rebuildTexSheet = false;
            var renderTexture = new RenderTexture(maxWidth, maxHeight, 0, RenderTextureFormat.ARGB32);

            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);

            for (var i = pcc.layerList.list.Count - 1; i >= 0; --i) {
                var layer = pcc.layerList.list[i];
                pccm.mat.SetColor(_color, layer.color);

                var tex = i != pcc.layerList.indexBody
                    ? layer.tex as Texture2D
                    : body;
                // calculate layer offset
                var offsetWidth = maxWidth - tex!.width;
                var offsetHeight = maxHeight - tex.height;

                VssMod.Log($"rebuilding layer {i}, tex {tex.width}x{tex.height}, offset {offsetWidth}x{offsetHeight}");

                // extend layer tex if necessary
                var extend = offsetWidth != 0 || offsetHeight != 0;
                if (extend) {
                    VssMod.Log(">> extending layer...");
                    tex = tex.ExtendBlit(maxWidth, maxHeight);
                    rebuildTexSheet = true;
                }

                Graphics.Blit(tex, renderTexture, pccm.mat);
            }

            var needRebuild = false;
            var variation = pcc.variation;

            if (!variation.tex || rebuildTexSheet ||
                variation.tex.width != renderTexture.width ||
                variation.tex.height != renderTexture.height) {
                if (variation.tex) {
                    Object.DestroyImmediate(variation.tex);
                    needRebuild = true;
                }

                var data = TextureImportSetting.Instance
                    ? TextureImportSetting.Instance.data
                    : IO.importSetting;
                variation.tex = new(renderTexture.width, renderTexture.height, data.format, data.mipmap, data.linear) {
                    wrapMode = data.wrapMode,
                    filterMode = data.filterMode,
                };
            }

            Graphics.CopyTexture(renderTexture, variation.tex);
            if (variation.main is null || needRebuild) {
                variation.BuildSprites(variation.tex);
            }

            VssMod.Log("finished rebuilding sprites");
        } catch (Exception ex) {
            VssMod.Log("failed rebuilding sprites");
            VssMod.Log(ex);
            return false;
        }

        return true;
    }
}