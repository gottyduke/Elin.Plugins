using System;
using Cwl.Helper.Unity;
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
}