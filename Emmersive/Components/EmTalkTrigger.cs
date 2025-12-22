using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Emmersive.API.Plugins;
using UnityEngine;
using UnityEngine.UI;

namespace Emmersive.Components;

internal class EmTalkTrigger : EMono
{
    private bool _defer;

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

    internal void ShowPlayerTalkDialog()
    {
        var d = Dialog.InputName(
            pc.Name,
            "",
            (cancel, text) => {
                if (cancel || text.IsWhiteSpaceOrNull) {
                    return;
                }

                EmKernel.Kernel!.GetRequiredService<SceneDirector>()
                    .DoPopText(player.uidChara, text);

                if (EmConfig.Policy.PlayerTalkTrigger.Value &&
                    EmScheduler.Mode != EmScheduler.SchedulerMode.Stop) {
                    // trigger immediately
                    this.StartDeferredCoroutine(() => EmScheduler.RequestScenePlayImmediate(), 0.01f);
                }
            },
            Dialog.InputType.None);

        d.input.field.characterLimit = 200;
        d.input.field.contentType = InputField.ContentType.Standard;
        d.input.field.text = "";

        // disable dark screen
        d.transform.GetChild(0).SetActive(false);
    }

    [CwlPostLoad]
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