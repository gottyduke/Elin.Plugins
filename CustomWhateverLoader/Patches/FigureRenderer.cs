using System.Collections.Generic;
using Cwl.API.Attributes;
using UnityEngine;

namespace Cwl.Patches;

// custom mob figure
internal class FigureRenderer : CardRenderer
{
    private static readonly Dictionary<string, CharaRenderer?> _cached = [];

    public override void Draw(RenderParam p, ref Vector3 v, bool drawShadow)
    {
        if (owner.trait is not TraitFigure figure ||
            !ShouldDrawFigure(out var renderer) ||
            renderer is null) {
            base.Draw(p, ref v, drawShadow);
            return;
        }

        var matColor = figure.GetMatColor();
        switch (matColor) {
            case -3:
                var matColors = core.Colors.matColors.TryGetValue("ether");
                p.matColor = BaseTileMap.GetColorInt(ref matColors!.main, 100) * -1;
                break;
            case < -3:
                p.matColor = matColor;
                break;
        }

        renderer.Draw(p, ref v, figure.ShowShadow);
    }

    private bool ShouldDrawFigure(out CharaRenderer? renderer)
    {
        renderer = null;

        if (!owner.IsInstalled && pc.held != owner && owner.ExistsOnMap && !owner.isRoofItem) {
            return false;
        }

        var refId = owner.c_idRefCard;
        if (refId is null) {
            return false;
        }

        // allow multiple figures from same id
        var key = $"{refId}_{owner.uid}";

        if (_cached.TryGetValue(key, out renderer)) {
            return renderer is not null;
        }

        if (!sources.charas.map.TryGetValue(refId, out var row) ||
            row.renderData.pass is not null) {
            _cached[key] = null;
            return false;
        }

        renderer = _cached[key] = new();
        renderer.SetOwner(CharaGen.Create(refId));

        return true;
    }

    [CwlThingOnCreateEvent]
    internal static void OnCreateFigure(Thing thing)
    {
        if (thing.trait is not TraitFigure) {
            return;
        }

        thing.renderer = new FigureRenderer();
        thing.renderer.SetOwner(thing);
    }
}