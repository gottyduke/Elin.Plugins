using System.ComponentModel;
using System.Text.RegularExpressions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Emmersive.Contexts;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using UnityEngine;

namespace Emmersive.API.Services.SceneDirector;

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

        content = chara.ApplyTone(content);
        content = content.Replace("~", "*");
        // gpt prefers this quote
        content = content.Replace("’", "'");

        var matches = Regex.Matches(content, @"(\*[^*]+\*)|([^\*]+)");

        foreach (Match match in matches) {
            if (!match.Success) {
                continue;
            }

            var text = match.Value.Trim();
            var gesture = text.StartsWith("*") && text.EndsWith("*");

            CoroutineHelper.Deferred(PopText, delay);
            continue;

            void PopText()
            {
                if (chara is not { isDestroyed: false, ExistsOnMap: true }) {
                    return;
                }

                Color color;
                if (gesture) {
                    color = Msg.colors.Ono;
                } else {
                    color = Msg.colors.Talk;
                    text = text.Replace("&", "").Bracket();
                }

                var logText = text.StripBrackets();
                RecentActionContext.Add(chara.Name, logText);

                Msg.SetColor(color);
                chara.Say(logText);

                chara.HostRenderer.Say(text.Wrap(), duration: duration);

                chara.Profile.SetTalked();
            }
        }

        EmMod.Debug<SceneDirector>($"{chara.Name}: (delay:{delay} duration:{duration}): {content}");
    }
}