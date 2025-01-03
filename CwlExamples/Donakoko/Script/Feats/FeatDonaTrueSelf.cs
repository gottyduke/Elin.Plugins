using System;
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
    
    internal void SyncLv()
    {
        if (owner.Chara?.id is not Constants.CharaId) {
            owner.Remove(id);
            return;
        }

        var cameraLv = owner.Chara.things.Find<TraitDonaCamera>()?.encLV ?? 0;
        vBase = Math.Min(cameraLv + 1, source.max);
    }
}