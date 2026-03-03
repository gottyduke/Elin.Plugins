using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cwl.Helper;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Emmersive.API.Plugins;
using UnityEngine;
using UnityEngine.UI;

namespace Emmersive.Components;

internal class EmTalkTrigger : EClass
{
    private bool _defer;

    // DISABLED: 23.282 Stable
    private void Update()
    {
        if (!Input.GetKeyDown(EmConfig.Policy.PlayerTalkKey.Value)) {
            return;
        }

        if (ui.TopLayer != null) {
            return;
        }

        if (_defer) {
            _defer = false;
            return;
        }

        ShowPlayerTalkDialog();

        _defer = true;
    }

    internal static Dialog ShowPlayerTalkDialog()
    {
        var d = Dialog.InputName(
            pc.Name,
            "",
            (cancel, text) => {
                if (cancel || text.IsWhiteSpaceOrNull) {
                    return;
                }


                var chara = pc;
                var canRequest = EmConfig.Policy.PlayerTalkTrigger.Value &&
                                 EmScheduler.Mode != EmScheduler.SchedulerMode.Stop;

                var m = Regex.Match(text, "^[＠@](?<idx>[0-9０-９])(.*)$");
                if (m.Success) {
                    var index = m.Groups["idx"].Value.Normalize(NormalizationForm.FormKC);
                    if (int.TryParse(index, out var result)) {
                        text = text[2..];
                        chara = pc.party.members.TryGet(result);
                    }
                }

                if (text.StartsWith("@")) {
                    text = text[1..];
                }

                chara ??= pc;
                if (text == "nyan") {
                    Msg.SetColor("save");
                    text = $"*{player.stats.lastChuryu} nyan*";
                    canRequest = false;
                }

                EmKernel.Kernel!.GetRequiredService<SceneDirector>()
                    .DoPopText(chara.uid, text);

                if (canRequest) {
                    // trigger immediately
                    core.StartDeferredCoroutine(() => EmScheduler.RequestScenePlayImmediate(), 0.01f);
                }
            },
            Dialog.InputType.None);

        d.input.field.characterLimit = 200;
        d.input.field.contentType = InputField.ContentType.Standard;
        d.input.field.text = "";

        // disable dark screen
        d.transform.GetChild(0).SetActive(false);

        return d;
    }

    // DISABLED: 23.282 Stable
    //[CwlPostLoad]
    private static void ShowModWarning()
    {
        const string chilemiaoId = "me.chilemiao.plugin.SaySomething";
        if (TypeQualifier.Plugins.FirstOrDefault(p => p.Info.Metadata.GUID == chilemiaoId) is null) {
            return;
        }

        Dialog.YesNo(
            "em_ui_warn_say_something",
            () => LayerEmmersivePanel.OpenPanelSesame("em_tab_prompt_setting"));
    }
}