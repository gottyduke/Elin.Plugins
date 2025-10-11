using System;
using System.Linq;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Patches.Effects;

[HarmonyPatch]
internal class ExtendedGunEffectPatch : EClass
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackProcess), nameof(AttackProcess.PlayRangedAnime))]
    internal static bool OnPlayDelayedEffect(AttackProcess __instance, int numFire, float delay)
    {
        if (__instance.weapon is not { } weapon) {
            return false;
        }

        var id = weapon.id;

        try {
            if (!DataLoader.CachedEffectData.TryGetValue(id, out var dataEx)) {
                return true;
            }

            var trait = __instance.toolRange;
            var mute = __instance.ignoreAttackSound;

            var isGun = trait is TraitToolRangeGun;
            var isCane = trait is TraitToolRangeCane;
            var isLaser = trait is TraitToolRangeGunEnergy || dataEx?.forceLaser is true;
            var isRail = isLaser && (id == "gun_rail" || dataEx?.forceRail is true);

            var fallback = isCane ? "cane" : isGun ? "gun" : "bow";
            var data = setting.effect.guns.TryGetValue(id, fallback);

            var cc = __instance.CC;
            var tp = __instance.posRangedAnime.Copy();

            // calculate the effect params before playing effects
            var caneColor = Color.white;
            if (isCane) {
                var eleSource = trait.owner.elements.dict.Values
                    .Where(e => e.source.categorySub == "eleAttack")
                    .RandomItem();
                if (eleSource is not null &&
                    Colors.elementColors.TryGetValue(eleSource.source.alias, out var eleColor)) {
                    caneColor = eleColor;
                }

                if (dataEx?.caneColor is not (null or "")) {
                    var overrideColor = dataEx.caneColor.ToColorEx();
                    caneColor = dataEx.caneColorBlend ? Color.Lerp(caneColor, overrideColor, 0.5f) : overrideColor;
                }
            }

            var gunEffect = data.idEffect.IsEmpty("gunfire");
            var fireSound = data.idSound.IsEmpty("attack_gun");
            var ejectSound = "bullet_drop";
            if (dataEx?.idSoundEject is not (null or "")) {
                ejectSound = dataEx.idSoundEject;
            }

            for (var i = 0; i < numFire; ++i) {
                TweenUtil.Delay(i * data.delay + delay, PlayGunEffect);
            }

            return false;

            void PlayGunEffect()
            {
                if (!core.IsGameStarted) {
                    return;
                }

                if (!cc.IsAliveInCurrentZone || cc.currentZone != _zone) {
                    return;
                }

                var ccPos = cc.isSynced ? cc.renderer.position : cc.pos.Position();
                var fireFromMuzzle = dataEx?.fireFromMuzzle is true;
                var pivot = cc.RendererDir is not (RendererDir.Down or RendererDir.Left) ? 1 : -1;
                var fireOffset = cc.IsPCC
                    ? data.firePos with { x = data.firePos.x * pivot }
                    : Vector2.zero;
                var fixedPos = (Vector3)fireOffset + ccPos;
                var fireFrom = cc.IsPCC && fireFromMuzzle
                    ? fixedPos
                    : ccPos;

                if (isLaser) {
                    var laserType = isRail ? "laser_rail" : "laser";
                    fireFrom = fireFromMuzzle ? fireOffset : Vector2.zero;
                    cc.PlayEffect(laserType, fix: fireFrom).GetComponent<SpriteBasedLaser>().Play(tp.PositionCenter());
                } else if (id == "gun_laser_assault") {
                    // newly added assault
                    Effect.Get("ranged_laser")._Play(cc.pos, fireFrom, to: tp, sprite: data.sprite);
                } else {
                    // projectiles are for non-laser weapons only
                    var projectile = Effect.Get("ranged_arrow");
                    if (isCane) {
                        projectile.sr.color = caneColor;
                    }

                    projectile._Play(cc.pos, fireFrom, to: tp, sprite: data.sprite);
                }

                if (isGun) {
                    if (cc.IsPCC) {
                        weapon.PlayEffect(gunEffect, true, 0f, fireOffset);
                    } else {
                        cc.PlayEffect(gunEffect);
                    }
                }

                if (data.eject) {
                    if (!mute) {
                        cc.PlaySound(ejectSound);
                    }

                    cc.PlayEffect("bullet").Emit(1);
                }

                if (!mute) {
                    cc.PlaySound(fireSound);
                }
            }
        } catch (Exception ex) {
            CwlMod.Warn<ExtendedGunEffectPatch>($"failed to apply gun effect {id}\n{ex}");

            return ThrowOrReturn.Return(ex, true);
        }
    }
}