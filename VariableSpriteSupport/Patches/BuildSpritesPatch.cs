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

        try {
            ReversePartId.BuildPartCache(pcc);

            // enforce 2:3 ratio for body tex
            if (body!.width / body.height != 32 / 48) {
                VssMod.Log($"body tex is not 2:3 ratio: {body.width}x{body.height}");
                return false;
            }

            // downscale body tex
            const int bodyWidth = 32 * 4;
            const int bodyHeight = 48 * 4;
            if (body is { width: > bodyWidth, height: > bodyHeight }) {
                VssMod.Log(
                    $"body tex is larger than {bodyWidth}x{bodyHeight}, downscaling from {body.width}x{body.height}");

                pccm.mat.SetColor(_color, pcc.layerList.list[pcc.layerList.indexBody].color);
                body = body.Downscale(bodyWidth, bodyHeight, pccm.mat);
            }

            // iterate all layers to get max
            var maxWidth = body.width;
            var maxHeight = body.height;
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