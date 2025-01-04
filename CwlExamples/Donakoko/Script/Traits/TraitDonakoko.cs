using Dona.Common;
using Dona.Feats;
using Dona.Stats;
using UnityEngine;

namespace Dona.Traits;

internal class TraitDonakoko : TraitUniqueChara
{
    // は隣接した敵の複製体を中立仲間として作り出す能力。
    // 複製体はオリジナルよりもLvが低く、ドナココ本人よりも敵に優先的に狙われる。
    internal void TakePhoto(Chara target)
    {
        if (target.hp == 0) {
            return;
        }

        var feat = owner.elements.GetOrCreateElement(Constants.FeatId) as FeatDonaTrueSelf;
        feat?.SyncLv();

        var featBonus = Mathf.Max((feat?.vBase ?? 1) - 1, 0) * 0.1f;
        var donaLv = owner.LV * (0.5f + featBonus);
        var targetLv = target.LV * 0.5f;

        var image = target.Duplicate();
        image.bio = target.bio;
        image._hobbies = target._hobbies;
        image._ability = target._ability;
        image._tactics = target._tactics;
        image.elements = target.elements;
        
        image.CalculateMaxStamina();
        image.stamina.value = Mathf.Min(image._maxStamina, target.stamina.value);
        image.mana.value = Mathf.Min(image.mana.value, image.mana.max);
        image.hp = Mathf.Min(image.hp, image.MaxHP);
        
        image.SetLv(Mathf.RoundToInt(donaLv + targetLv));
        image.c_altName = target.Name;
        image._alias = "dona_image_prefix".lang();

        image.MakeMinion(pc);

        _zone.AddCard(image, target.pos.GetNearestPoint(allowChara: false));
        image.PlaySound("identify");
        image.PlayEffect("teleport");
        
        image.AddCondition<ConDonaAfterImage>();
        image.AddCondition<StanceTaunt>();
        image.tactics.source.taunt = 1;
    }
}