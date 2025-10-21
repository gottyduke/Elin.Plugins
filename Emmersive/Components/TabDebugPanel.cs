using System.Linq;
using Cwl.Helper.FileUtil;
using Emmersive.Contexts;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabDebugPanel : TabEmmersiveBase
{
    public override void OnLayout()
    {
        BuildDebugButtons();

        var logs = RecentActionContext.RecentActions
            .TakeLast(EmConfig.Context.RecentLogDepth.Value * 2)
            .Reverse()
            .ToArray();

        if (logs.Length > 0) {
            var logPanel = this.MakeCard();
            logPanel.HeaderCard("em_ui_recent_action");

            var langWidth = Lang.isEN ? 15f : 18f;
            var max = logs.Max(a => a.actor.Length) / EMono.ui.canvasScaler.scaleFactor * langWidth;

            foreach (var (actor, text) in logs) {
                var pair = logPanel.TopicPair(actor, text);
                pair.text1.alignment = TextAnchor.UpperLeft;
                pair.text1.GetOrCreate<LayoutElement>().preferredWidth = max;
            }
        }

        var activities = EmActivity.Session
            .TakeLast(15)
            .Reverse()
            .ToArray();

        if (activities.Length > 0) {
            var requestPanel = this.MakeCard();
            requestPanel.HeaderCard("em_ui_recent_requests");

            foreach (var activity in activities) {
                var entry = requestPanel.Horizontal()
                    .WithSpace(5);
                entry.Layout.childForceExpandWidth = true;

                entry.TopicPair(activity.RequestTime.ToLocalTime().ToLongTimeString(), activity.ServiceName);
                entry.TopicPair(activity.Status.ToString(), $"{activity.TokensInput} + {activity.TokensOutput}");
            }
        }

        base.OnLayout();
    }

    private void BuildDebugButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_test_generation".lang(), () => {
            LayerEmmersivePanel.Instance!.OnLayoutConfirm();
            EmScheduler.RequestScenePlayImmediate();
            ELayer.ui.RemoveLayer<LayerEmmersivePanel>();
        });

        btnGroup.Button("em_ui_scheduler_dry".lang(), () => {
            EmScheduler.SwitchMode(EmScheduler.ScheduleMode.DryRun);
            EmScheduler.RequestScenePlayImmediate();
        });

        btnGroup.Button("em_ui_config_open".lang(), () => OpenFileOrPath.Run(EmMod.Instance.Config.ConfigFilePath));
    }
}