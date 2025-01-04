using System.Collections;
using Dona.Common;
using UnityEngine;

namespace Dona.Stats;

// the main condition to monitor the after image duration and effects
internal class ConDonaAfterImage : Condition
{
    private Coroutine? _effectRenderer;

    public override bool CanManualRemove => false;

    // when applied, show text to indicate the image creation
    public override void OnStart()
    {
        Msg.SetColor(Msg.colors.Ono);
        Msg.Say(source.GetText("textPhase"), owner);

        // override the duration with config value
        value = DonaConfig.ImageDuration?.Value ?? 100;
    }

    public override void OnValueChanged()
    {
        if (value > 0) {
            // if effect renderer hasn't started yet, instantiate new one
            _effectRenderer ??= core.StartCoroutine(RenderPartialEffects());
            // set the image as summon, but with +1 duration
            // so that OnRemoved effects are triggered before summon.Die event
            owner.SetSummon(value + 1);
            return;
        }

        // condition ticked to 0, time to kill
        Msg.SetColor(Msg.colors.Ono);
        Kill();
        Msg.SetColor();
    }

    public override void OnRemoved()
    {
        owner.PlayEffect("vanish");
        owner.PlaySound("vanish");
        owner.Destroy();

        // halt the effect renderer (it should be stopped already)
        core.StopCoroutine(_effectRenderer);
    }

    private IEnumerator RenderPartialEffects()
    {
        // randomize start delay, so that multiple images don't synchronize in effects
        yield return new WaitForSeconds(rndf(Constants.EffectFrameSkip * 2));

        // if image is alive and on the same map with pc
        while (owner is { isDestroyed: false, ExistsOnMap: true }) {
            // only render the effect if pc can see the image
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
                    // noexcept
                }
            }

            yield return new WaitForSeconds(Constants.EffectFrameSkip);
        }

        // if owner is alive and no longer on the same map with pc
        if (owner is { isDestroyed: false, ExistsOnMap: false }) {
            // destroy the image
            owner.RemoveCondition<ConDonaAfterImage>();
        }
    }
}