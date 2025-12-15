using System;
using System.IO;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using ReflexCLI.Attributes;
using Object = UnityEngine.Object;

namespace Cwl;

internal partial class DataLoader
{
    internal const string MediaPathEntry = "Media/Graphics";

    internal static bool RelocateSprite(string path, ref Object? loaded)
    {
        if (!path.StartsWith(MediaPathEntry, StringComparison.InvariantCultureIgnoreCase)) {
            return false;
        }

        var texId = path.Split('/')[^1];
        var sprite = texId.LoadSprite();
        if (sprite == null) {
            return false;
        }

        loaded = sprite;
        CwlMod.Debug<DataLoader>($"Relocated sprite {texId} > {sprite.name}");

        return true;
    }

    [ConsoleCommand("load_sprites")]
    internal static void RefreshAllPackageTextures()
    {
        var textures = PackageIterator.GetRelocatedDirsFromPackage("Texture");
        foreach (var textureDir in textures) {
            foreach (var texture in textureDir.GetFiles("*.png", SearchOption.AllDirectories)) {
                var container = Path.GetRelativePath(textureDir.FullName, texture.FullName).NormalizePath();
                var id = Path.ChangeExtension(container, null).NormalizePath();
                SpriteReplacer.dictModItems[id] = Path.ChangeExtension(texture.FullName, null).NormalizePath();
            }
        }
    }
}