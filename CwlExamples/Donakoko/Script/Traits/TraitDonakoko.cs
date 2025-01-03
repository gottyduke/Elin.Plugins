using System.Collections;
using Cwl.Helper.Unity;
using Dona.Common;
using Dona.Feats;
using Dona.Stats;
using UnityEngine;

namespace Dona.Traits;

internal class TraitDonakoko : TraitUniqueChara
{
    // デジカメ, あちこち擦り切れているが、レンズは丁寧に手入れがされている。
    // ステータスを上昇、首は他の装備をつけれない
    internal void TakePhoto(Chara target)
    {
        var feat = owner.elements.GetOrCreateElement(Constants.FeatId) as FeatDonaTrueSelf;
        feat?.SyncLv();
        
        var featBonus = Mathf.Max((feat?.vBase ?? 1) - 1, 0) * 0.1f;
        var targetLv = target.LV * 0.5f;
        var donaLv = owner.LV * (0.5f + featBonus);
        
        var image = CharaGen.Create(target.id, Mathf.RoundToInt(donaLv + targetLv));
        image.MakeMinion(owner);
        
        _zone.AddCard(image, target.pos.GetNearestPoint(allowChara: false));
        image.PlaySound("identify");
        image.PlayEffect("teleport");
        
        image.AddCondition<ConDonaAfterImage>();
        image.AddCondition<StanceTaunt>();
        image.tactics.source.taunt = 1;

        core.StartCoroutine(RenderPartialEffects(image));
    }

    private static IEnumerator RenderPartialEffects(Chara? target)
    {
        while (target is { isDestroyed: false, ExistsOnMap: true }) {
            if (pc.CanSee(target)) {
                try {
                    var effect = Effect.Get("rod");

                    var pos = target.isSynced
                        ? target.GetRootCard().renderer.position
                        : target.GetRootCard().pos.Position();
                    effect._Play(target.GetRootCard().pos, pos, sprite: effect.sprites[5]);
                } catch {
                    yield break;
                }
            }
            
            yield return new WaitForSeconds(Constants.EffectFrameSkip);
        }
    }
}