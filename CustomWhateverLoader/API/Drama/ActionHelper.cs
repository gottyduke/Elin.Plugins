using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static void AddTempTalk(string text, string actor = "tg", string? jump = null)
    {
        if (Cookie?.Dm is not { } dm ||
            (jump is not null && !dm.sequence.steps.ContainsKey(jump))) {
            return;
        }

        var talkEvent = new DramaEventTalk {
            idActor = actor,
            idJump = jump ?? dm.sequence.lastlastStep.IsEmpty("end"),
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

    public static void InjectUniqueRumor()
    {
        if (Cookie?.Dm is not { } dm ||
            dm.tg?.hasChara is not true) {
            return;
        }

        var chara = dm.tg.chara;
        var rumors = Lang.GetDialog("unique", chara.id);
        var hasTopic = rumors.Length > 1 || rumors.TryGet(0, true) != chara.id;
        var animalTalk = chara.IsHumanSpeak || pc.HasElement(FEAT.featAnimalLover);

        if (!hasTopic || !animalTalk || !dm.customEventsAdded) {
            return;
        }

        dm.CustomEvent(dm.sequence.Exit);

        var choice = new DramaChoice("letsTalk".lang(), dm.sequence.steps.Last().Key.IsEmpty(dm.setup.step));
        dm.lastTalk.choices.Add(choice);
        dm._choices.Add(choice);

        var rumor = chara.GetUniqueRumor(dm.enableTone);
        choice.SetOnClick(() => {
            var firstText = rumor;
            dm.sequence.firstTalk.funcText = () => firstText;
            rumor = chara.GetUniqueRumor(dm.enableTone);
            chara.affinity.OnTalkRumor();
            choice.forceHighlight = true;
        }).SetCondition(() => chara.interest > 0);
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
            var methodGroup = $"[{action.Method.Name}]({string.Join(",", pack)})";
            CwlMod.WarnWithPopup<DramaExpansion>($"call failure: {methodGroup}\n{ex.Message}", ex);
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
}