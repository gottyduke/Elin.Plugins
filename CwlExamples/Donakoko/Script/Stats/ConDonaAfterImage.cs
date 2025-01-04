using System.Collections;
using Dona.Common;
using UnityEngine;

namespace Dona.Stats;

internal class ConDonaAfterImage : Condition
{
    private Coroutine? _effectRenderer;

    public override bool CanManualRemove => false;

    public override void OnStart()
    {
        Msg.SetColor(Msg.colors.Ono);
        Msg.Say(source.GetText("textPhase"), owner);

        value = DonaConfig.ImageDuration?.Value ?? 100;
    }

    public override void OnValueChanged()
    {
        if (value > 0) {
            _effectRenderer ??= core.StartCoroutine(RenderPartialEffects());
            owner.SetSummon(value + 1);
            return;
        }

        Msg.SetColor(Msg.colors.Ono);
        Kill();
        Msg.SetColor();
    }

    public override void OnRemoved()
    {
        owner.PlayEffect("vanish");
        owner.PlaySound("vanish");
        owner.Destroy();

        core.StopCoroutine(_effectRenderer);
    }

    private IEnumerator RenderPartialEffects()
    {
        yield return new WaitForSeconds(rndf(Constants.EffectFrameSkip));

        while (owner is { isDestroyed: false, ExistsOnMap: true }) {
            if (pc.CanSee(owner)) {
                try {
                    var effect = Effect.Get("rod");
                    effect.speed *= 0.5f;

                    var pos = owner.isSynced
                        ? owner.GetRootCard().renderer.position
                        : owner.GetRootCard().pos.Position();
                    effect._Play(owner.GetRootCard().pos, pos, sprite: effect.sprites[5]);
                } catch {
                    yield break;
                }
            }

            yield return new WaitForSeconds(Constants.EffectFrameSkip);
        }

        if (owner is { isDestroyed: false, ExistsOnMap: false }) {
            owner.RemoveCondition<ConDonaAfterImage>();
        }
    }
}