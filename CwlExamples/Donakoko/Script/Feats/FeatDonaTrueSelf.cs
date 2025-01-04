using System;
using Cwl.LangMod;
using Dona.Common;
using Dona.Traits;
using UnityEngine;

namespace Dona.Feats;

internal class FeatDonaTrueSelf : Feat
{
    // reroute the icon to the dona feat icon
    public override Sprite GetIcon(string suffix = "")
    {
        return SpriteSheet.Get(source.alias);
    }

    // append extra info about dona's feat bonus
    public override void OnWriteNote(UINote n, ElementContainer eleOwner)
    {
        base.OnWriteNote(n, eleOwner);
        
        n.AddText("_bullet".lang() + "dona_feat_base".lang());
        if (vBase > 0) {
            n.AddText("_bullet".lang() + "dona_feat_bonus".Loc(vBase * 10));
        }
    }

    // invoked by CWL to sync level
    internal void _OnApply(int add, ElementContainer eleOwner, bool hint)
    {
        SyncLv();
    }

    // sync the level with camera
    internal void SyncLv()
    {
        // only dona can have this feat
        if (owner.Chara?.id is not Constants.CharaId) {
            // remove the feat from owner if it's not dona
            owner.Remove(id);
            return;
        }

        // get camera's enchant level
        var cameraLv = owner.Chara.things.Find<TraitDonaCamera>()?.encLV ?? 0;
        // set feat level to camera level
        vBase = Math.Min(cameraLv, source.max);
    }
}