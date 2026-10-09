using System.Text;
using System.Text.RegularExpressions;
using Emmersive.API.Plugins;
using Emmersive.Helper;
using UnityEngine.UI;

namespace Emmersive.Components;

internal class EmTalkTrigger : EClass
{
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
                        text = text[2..].Trim();
                        chara = pc.party.members.TryGet(result);
                    }
                }

                if (text.StartsWith("@")) {
                    text = text[1..];
                }

                if (text.IsWhiteSpaceOrNull) {
                    return;
                }

                chara ??= pc;
                if (text == "nyan") {
                    Msg.SetColor("save");
                    text = $"*{player.stats.lastChuryu} nyan*";
                    canRequest = false;
                }

                var isPlayer = chara.IsPC;

                SceneDirector.Instance.DoPopText(chara.uid, text, isPlayer: isPlayer);

                if (canRequest) {
                    // trigger immediately
                    EmScheduler.OnTalkTrigger(new() {
                        Chara = chara,
                        Trigger = text,
                        IsPlayer = isPlayer,
                        AlreadyShown = true,
                    });
                }
            },
            Dialog.InputType.None);

        d.input.field.characterLimit = 200;
        d.input.field.contentType = InputField.ContentType.Standard;
        d.input.field.text = "";
        UIHelper.SetPlaceholder(d.input.field, "em_ui_chat_hint".lang());

        // disable dark screen
        d.transform.GetChild(0).SetActive(false);

        return d;
    }
}