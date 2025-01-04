using System;
using Cwl.LangMod;
using Dona.Common;
using Dona.Traits;
using UnityEngine;

namespace Dona.Feats;

internal class FeatDonaTrueSelf : Feat
{
    public override Sprite GetIcon(string suffix = "")
    {
        return SpriteSheet.Get(source.alias);
    }

    public override void OnWriteNote(UINote n, ElementContainer eleOwner)
    {
        base.OnWriteNote(n, eleOwner);
        
        n.AddText("_bullet".lang() + "dona_feat_base".lang());
        if (vBase > 0) {
            n.AddText("_bullet".lang() + "dona_feat_bonus".Loc(vBase * 10));
        }
    }

    internal void _OnApply(int add, ElementContainer eleOwner, bool hint)
    {
        SyncLv();
    }

    internal void SyncLv()
    {
        if (owner.Chara?.id is not Constants.CharaId) {
            owner.Remove(id);
            return;
        }

        var cameraLv = owner.Chara.things.Find<TraitDonaCamera>()?.encLV ?? 0;
        vBase = Math.Min(cameraLv, source.max);
    }
}