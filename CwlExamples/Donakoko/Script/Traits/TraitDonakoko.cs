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
        // clamp and set level
        var imageLv = Mathf.Min(Mathf.RoundToInt(donaLv + targetLv),
            owner.LV * DonaConfig.ImageLevelRatioCap?.Value ?? 20);

        // make a duplicate of the target as image
        var image = target.Duplicate();
        image.SetLv(imageLv);

        // sync elements with modifier
        image.elements.dict.Clear();
        var modifier = (float)imageLv / target.LV;
        foreach (var (id, src) in target.elements.dict) {
            var ele = image.elements.GetOrCreateElement(id);
            ele.vBase = Mathf.RoundToInt(src.vBase * modifier);
            ele.vLink = Mathf.RoundToInt(src.vLink * modifier);
            ele.vSource = Mathf.RoundToInt(src.vSource * modifier);
        }

        // if target is already dead, scale hp back
        if (target.hp <= 0) {
            image.hp = Mathf.RoundToInt(image.MaxHP * modifier);
        }

        // sync metadata, this is for extend display users
        image.bio = target.bio;
        image._hobbies = target._hobbies;
        image._ability = target._ability;
        image._tactics = target._tactics;
        image._job = target._job;
        image._works = target._works;

        // sync stats
        image.mana.value = Mathf.Min(image.mana.value, image.mana.max);
        image.hp = Mathf.Min(image.hp, image.MaxHP);

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