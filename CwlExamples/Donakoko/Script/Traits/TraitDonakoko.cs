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
        // if target is already dead, don't make image
        if (target.hp <= 0) {
            return;
        }

        // get koko's feat
        var feat = owner.elements.GetOrCreateElement(Constants.FeatId) as FeatDonaTrueSelf;
        // sync base level with koko camera
        feat?.SyncLv();

        // each feat bonus gives 10% extra level
        var featBonus = Mathf.Max(feat?.vBase ?? 0, 0) * 0.1f;
        // base level = dona.LV * (50% + featBonus)
        var donaLv = owner.LV * (0.5f + featBonus);
        // target level * 50%
        var targetLv = target.LV * 0.5f;

        // make a duplicate of the target as image
        var image = target.Duplicate();
        
        // sync meta data
        image.bio = target.bio;
        image._hobbies = target._hobbies;
        image._ability = target._ability;
        image._tactics = target._tactics;
        image.elements = target.elements;
        
        // sync stats
        image.CalculateMaxStamina();
        image.stamina.value = Mathf.Min(image._maxStamina, target.stamina.value);
        image.mana.value = Mathf.Min(image.mana.value, image.mana.max);
        image.hp = Mathf.Min(image.hp, image.MaxHP);
        
        // set level
        image.SetLv(Mathf.RoundToInt(donaLv + targetLv));
        
        // set name with image prefix
        image.c_altName = target.Name;
        image._alias = "dona_image_prefix".lang();

        // set as pc's minion
        image.MakeMinion(pc);

        // spawn next to target
        _zone.AddCard(image, target.pos.GetNearestPoint(allowChara: false));
        image.PlaySound("identify");
        image.PlayEffect("teleport");
        
        // add after image condition to track the duration and effects
        image.AddCondition<ConDonaAfterImage>();
        // set the tactics to taunt > 1% hp
        image.AddCondition<StanceTaunt>();
        image.tactics.source.taunt = 1;
    }
}