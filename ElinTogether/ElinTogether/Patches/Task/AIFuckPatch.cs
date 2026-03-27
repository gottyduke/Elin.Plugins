using System;
using System.Collections.Generic;
using System.Net.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches.Task;

[HarmonyPatch]
internal static class AIFuckPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AI_Fuck), nameof(AI_Fuck.Run))]
    internal static bool OnRun(AI_Fuck __instance, ref IEnumerable<AIAct.Status> __result)
    {
        __result = Run_Modified(__instance);
        return false;
    }

    internal static IEnumerable<AIAct.Status> Run_Modified(AI_Fuck thiz)
    {
        if (thiz.target == null) {
            foreach (var chara in EClass._map.charas) {
                if (!chara.IsHomeMember() && !chara.IsDeadOrSleeping && chara.Dist(thiz.owner) <= 5) {
                    thiz.target = chara;
                    break;
                }
            }
        }
        if (thiz.target == null) {
            yield return thiz.Cancel();
        }
        var cc = (thiz.sell ? thiz.target : thiz.owner)!;
        var tc = (thiz.sell ? thiz.owner : thiz.target)!;
        var destDist = (thiz.Type == AI_Fuck.FuckType.fuck) ? 1 : 1;
        if (thiz.owner.host != thiz.target) {
            yield return thiz.DoGoto(thiz.target!.pos, destDist, ignoreConnection: true);
        }
        cc.Say((thiz.variation == AI_Fuck.Variation.Slime) ? "slime_start" : ((thiz.variation == AI_Fuck.Variation.Bloodsuck) ? "suck_start" : (thiz.Type.ToString() + "_start")), cc, tc);
        thiz.isFail = () => !tc.IsAliveInCurrentZone || tc.Dist(thiz.owner) > 3;
        if (thiz.Type == AI_Fuck.FuckType.tame) {
            cc.SetTempHand(1104, -1);
        }
        switch (thiz.variation) {
            case AI_Fuck.Variation.Succubus:
                cc.Talk("seduce");
                break;
            case AI_Fuck.Variation.Bloodsuck:
                cc.PlaySound("bloodsuck");
                break;
            case AI_Fuck.Variation.Slime:
                cc.PlaySound("slime");
                thiz.target.AddCondition<ConEntangle>(500, force: true);
                break;
        }
        var maxProgress = (thiz.variation == AI_Fuck.Variation.NTR || thiz.variation == AI_Fuck.Variation.Bloodsuck) ? 10 : 25;
        var p = new Progress_Custom {
            interval = 1,
            maxProgress = maxProgress,
            showProgress = false,
            canProgress = thiz.CanProgress,
            onProgress = p => {
                if (thiz.owner.Dist(thiz.target) > 1) {
                    thiz.owner.TryMoveTowards(thiz.target.pos);
                    return;
                }

                var i = p.progress;
                if (i < 0) {
                    i += int.MaxValue;
                }
                switch (thiz.Type) {
                    case AI_Fuck.FuckType.fuck:
                        if (thiz.variation == AI_Fuck.Variation.NTR) {
                            cc.Say("ntr", cc, tc);
                        }
                        cc.LookAt(tc);
                        tc.LookAt(cc);
                        switch (i % 4) {
                            case 0:
                                cc.renderer.PlayAnime(AnimeID.Attack, tc);
                                if (EClass.rnd(3) == 0 || thiz.sell) {
                                    cc.Talk("tail");
                                }
                                break;
                            case 2:
                                tc.renderer.PlayAnime(AnimeID.Shiver);
                                if (EClass.rnd(3) == 0) {
                                    tc.Talk("tailed");
                                }
                                break;
                        }
                        if (((cc.HasElement(1216) || tc.HasElement(1216)) ? 100 : 20) > EClass.rnd(100)) {
                            ((EClass.rnd(2) == 0) ? cc : tc).PlayEffect("love2");
                        }
                        if (thiz.variation == AI_Fuck.Variation.Slime) {
                            thiz.owner.DoHostileAction(thiz.target);
                        }
                        if (EClass.rnd(3) == 0 || thiz.sell) {
                            if (thiz.variation == AI_Fuck.Variation.Slime) {
                                thiz.target.AddCondition<ConSupress>(200, force: true);
                            } else {
                                thiz.target.AddCondition<ConWait>(50, force: true);
                            }
                        }
                        if (thiz.variation == AI_Fuck.Variation.Bloodsuck || thiz.variation == AI_Fuck.Variation.Slime) {
                            thiz.owner.pos.TryWitnessCrime(cc, tc, 4, c => EClass.rnd(cc.HasCondition<ConTransmuteBat>() ? 50 : 20) == 0);
                        }
                        break;
                    case AI_Fuck.FuckType.tame:
                        var num = 100;
                        if (!tc.IsAnimal) {
                            num += 50;
                        }
                        if (tc.IsHuman) {
                            num += 50;
                        }
                        if (tc.IsInCombat) {
                            num += 100;
                        }
                        if (tc == cc) {
                            num = 50;
                        } else if (tc.affinity.CurrentStage < Affinity.Stage.Intimate && EClass.rnd(6 * num / 100) == 0) {
                            tc.AddCondition<ConFear>(60);
                        }
                        tc.interest -= tc.IsPCFaction ? 20 : (2 * num / 100);
                        if (i == 0 || i == 10) {
                            cc.Talk("goodBoy");
                        }
                        if (i % 5 == 0) {
                            tc.PlaySound("brushing");
                            var num2 = cc.CHA / 2 + cc.Evalue(237) - tc.CHA * 2;
                            int num3;
                            if (EClass.rnd(cc.CHA / 2 + cc.Evalue(237)) > EClass.rnd(tc.CHA * num / 100)) {
                                num3 = 5 + Mathf.Clamp(num2 / 20, 0, 20);
                            } else {
                                num3 = -5 + ((!tc.IsPCFaction) ? Mathf.Clamp(num2 / 10, -30, 0) : 0);
                                thiz.fails++;
                            }
                            var num4 = 20;
                            if (tc.IsPCFactionOrMinion && tc.affinity.CurrentStage >= Affinity.Stage.Love) {
                                num3 = (EClass.rnd(3) == 0) ? 4 : 0;
                                num4 = 10;
                            }
                            thiz.totalAffinity += num3;
                            tc.ModAffinity(EClass.pc, num3, show: true, showOnlyEmo: true);
                            cc.elements.ModExp(237, num4);
                            if (EClass.rnd(4) == 0) {
                                cc.stamina.Mod(-1);
                            }
                        }
                        break;
                }
            },
            onProgressComplete = thiz.Finish,
        };
        yield return thiz.Do(p);
    }
}