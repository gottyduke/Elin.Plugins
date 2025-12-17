using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.LangMod;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static void AddTempTalk(string text, string actor = "tg", string? jump = null)
    {
        if (Cookie?.Dm is not { } dm ||
            jump is not null && !dm.sequence.steps.ContainsKey(jump)) {
            return;
        }

        var talkEvent = new DramaEventTalk {
            idActor = actor,
            idJump = jump ?? dm.sequence.lastlastStep.OrIfEmpty("end"),
            text = text,
            temp = true,
            sequence = dm.sequence,
        };

        dm.lastTalk = talkEvent;
        dm.AddEvent(talkEvent);
        dm.sequence.tempEvents.Add(dm.lastTalk);
    }

    public static void Goto(string step)
    {
        if (Cookie?.Dm is not { } dm || !dm.sequence.steps.ContainsKey(step)) {
            return;
        }

        dm.sequence.Play(step);
    }

    public static void InjectUniqueRumor(DramaManager? dm = null)
    {
        dm ??= Cookie?.Dm;
        if (dm?.tg?.hasChara is not true) {
            return;
        }

        var chara = dm.tg.chara;
        var canTalk = chara.IsHumanSpeak || pc.HasElement(FEAT.featAnimalLover);

        if (!chara.HasRumorText("unique") || !canTalk || !dm.customEventsAdded) {
            return;
        }

        dm.CustomEvent(dm.sequence.Exit);

        var choice = new DramaChoice("letsTalk".lang(), dm.sequence.steps.Last().Key.OrIfEmpty(dm.setup.step));
        dm.lastTalk.choices.Add(choice);
        dm._choices.Add(choice);

        var rumor = chara.GetUniqueRumor();
        choice
            .SetOnClick(() => {
                var firstText = rumor;
                dm.sequence.firstTalk.funcText = () => firstText;
                rumor = chara.GetUniqueRumor();
                chara.affinity.OnTalkRumor();
                choice.forceHighlight = true;
            })
            .SetCondition(() => chara.interest > 0);
    }

    private static bool SafeInvoke(ActionWrapper action, DramaManager dm, Dictionary<string, string> item, params string[] pack)
    {
        try {
            Cookie = new(dm, item);
            var result = action.Method.FastInvokeStatic(dm, item, pack);
            return result is true;
        } catch (DramaActorMissingException) {
            return false;
            // noexcept
        } catch (Exception ex) {
            if (ex is not DramaException) {
                ex = new DramaException(ex.Message);
            }

            var methodGroup = $"[{action.Method.Name}]({string.Join(",", pack)})";
            CwlMod.WarnWithPopup<DramaExpansion>("cwl_warn_drama_call_ex".Loc(methodGroup, ex.Message), ex);
            // noexcept
        }

        return false;
    }

    public static float ArithmeticModOrSet(float lhs, string expr)
    {
        return expr.ArithmeticModOrSet(lhs);
    }

    public static int ArithmeticModOrSet(int lhs, string expr)
    {
        return expr.ArithmeticModOrSet(lhs);
    }

    public static float ArithmeticDiff(float lhs, string expr)
    {
        return expr.ArithmeticDiff(lhs);
    }

    public static int ArithmeticDiff(int lhs, string expr)
    {
        return expr.ArithmeticDiff(lhs);
    }

    public static bool Compare(float lhs, string expr)
    {
        return expr.Compare(lhs);
    }

    public static bool Compare(int lhs, string expr)
    {
        return expr.Compare(lhs);
    }

    public static bool Compare(object lhs, string expr)
    {
        return lhs switch {
            byte b => Compare(b, expr),
            sbyte b => Compare(b, expr),
            short s => Compare(s, expr),
            ushort s => Compare(s, expr),

            int i => Compare(i, expr),
            uint i => Compare((int)i, expr),

            long l => Compare((int)l, expr),
            ulong l => Compare((int)l, expr),

            float f => Compare(f, expr),
            double f => Compare((float)f, expr),
            decimal d => Compare((float)d, expr),

            bool b => b == bool.Parse(expr),

            char c => c.ToString() == expr,
            string s => string.Equals(s, expr, StringComparison.OrdinalIgnoreCase),

            _ => string.Equals(lhs.ToString(), expr, StringComparison.OrdinalIgnoreCase),
        };
    }
}