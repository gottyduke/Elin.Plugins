using System;
using System.ComponentModel;
using Cwl.Helper.String;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using UnityEngine;

namespace Emmersive.API.Plugins.SceneDirector;

public partial class SceneDirector
{
    [KernelFunction("character_bubble")]
    [Description("Displays a dialogue, gesture, or thought above character's head. Use multiple if needed, DO NOT mix contents.")]
    public void DoPopText([Description("The unique identifier (uid) of the character who will speak or act.")] int uid,
                          [Description("The text to be displayed. Use a brief, single line. " +
                                       "Gesture and thought should be concise and contains only a few words. " +
                                       "Do not include quotation marks. " +
                                       "Unity rich text tags are supported.")]
                          string content,
                          [Description("How long, in seconds, the text bubble should remain visible.")] float duration = 2.5f,
                          [Description("Delay, in seconds, before executing this action. Use it to chain actions naturally.")]
                          float delay = 0f)
    {
        if (!FindSameMapChara(uid, out var chara)) {
            return;
        }

        DeferAction(PopText, delay);
        EmMod.Debug<SceneDirector>($"{chara.Name} says (delay: {delay} duration: {duration}): {content}");

        return;

        void PopText()
        {
            content = chara.ApplyTone(content);
            content = content.Replace("~", "*");
            content = content.Replace("&", "");

            var gesture = content.StartsWith("*");
            var think = content.StartsWith("(");

            if (chara.IsPCParty) {
                duration -= 0.6f;
            }

            duration = Mathf.Max(duration, 0f);

            Color color;
            if (gesture) {
                color = Msg.colors.Ono;
            } else if (think) {
                color = Msg.colors.Thinking;
                content = $"({content})";
            } else {
                color = Msg.colors.Talk;
                content = content.Bracket();
            }

            Msg.SetColor(color);
            chara.Say(content);

            chara.HostRenderer.Say(content.StripBrackets().Wrap(), duration: duration);

            var profile = chara.Profile;
            profile.LastReactionTime = DateTime.Now;
            profile.LastReactionTurn = chara.turn;
        }
    }
}