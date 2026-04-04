using System.Diagnostics.CodeAnalysis;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class InvSplitThingEvent : EClass
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Thing), nameof(Thing.ShowSplitMenu))]
    internal static bool OnShowSplitMenu(Thing __instance, ButtonGrid button, InvOwner.Transaction? trans)
    {
        var count = 1;
        var m = ui.CreateContextMenuInteraction();
        var buy = trans is not null;

        UIButton? buttonBuy = null;
        UIItem? itemSlider = null;

        itemSlider = m.AddSlider(
                "sliderSplitMenu",
                "adjustmentNum",
                _ => !core.IsGameStarted ? "" : $"/{__instance.Num}",
                count,
                b => {
                    count = (int)b;
                    trans?.num = count;
                    UpdateButton();
                },
                1f, __instance.Num, true, false, true)
            .GetComponent<UIItem>();

        if (buy) {
            buttonBuy = m.AddButton("invBuy", Process);
        }

        m.onDestroy = () => {
            if ((!buy || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !m.wasCanceled) {
                Process();
            }
        };
        m.Show();

        if (buttonBuy != null) {
            buttonBuy.gameObject.AddComponent<CanvasGroup>();
        }

        UpdateButton();

        return false;

        void Process()
        {
            if (!core.IsGameStarted || button == null || button.card is null) {
                Debug.Log("Split bug1");
            } else if (button.card.isDestroyed || button.card.Num < count) {
                Debug.Log("Split bug2");
            } else if (pc.isDead) {
                Debug.Log("Split bug3");
            } else if (count != 0 && !Input.GetMouseButton(1)) {
                if (trans is not null) {
                    trans.Process(startTransaction: true);
                } else {

                    var dragItemCard = new DragItemCard(button);

                    // ??
                    if (NetSession.Instance.IsHost) {
                        dragItemCard.from.thing = __instance;
                        ui.StartDrag(dragItemCard);
                        return;
                    }

                    ThingRequest
                        .Create(dragItemCard.from.thing, __instance.Num)
                        .Then(thing => {
                            dragItemCard.from.thing = thing;
                            ui.StartDrag(dragItemCard);
                        });

                    // ??
                    if (count != __instance.Num) {
                        var thing = button.card.Split(__instance.Num - count);
                        button.invOwner.Container.AddThing(thing, tryStack: false);
                        thing.invX = dragItemCard.from.invX;
                        thing.invY = dragItemCard.from.invY;
                        thing.posInvX = button.card.Thing.posInvX;
                        thing.posInvY = button.card.Thing.posInvY;
                    }
                    ui.StartDrag(dragItemCard);
                }
            }
        }

        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        void UpdateButton()
        {
            if (itemSlider != null) {
                itemSlider.text1.text = __instance.GetName(NameStyle.FullNoArticle, 1);
                itemSlider.text2.text = Lang._weight(__instance.SelfWeight * count);
            }

            if (buttonBuy != null && trans is not null) {
                buttonBuy.mainText.SetText(trans.GetTextDetail());
                buttonBuy.mainText.RebuildLayoutTo<UIButton>();
                buttonBuy.interactable = trans.IsValid();
                buttonBuy.RebuildLayout(recursive: true);
                buttonBuy.gameObject.GetComponent<CanvasGroup>().alpha = (trans.IsValid() ? 1f : 0.9f);
            }
        }
    }
}