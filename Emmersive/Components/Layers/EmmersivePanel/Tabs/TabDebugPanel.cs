using System.Linq;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabDebugPanel : TabEmmersiveBase
{
    public override void OnLayout()
    {
        var allEntries = MemoryManager.Instance.AllStores
            .SelectMany(s => s.GetRecentStm(EmConfig.Memory.MaxStmInContext.Value))
            .OrderByDescending(e => e.Turn)
            .Take(EmConfig.Context.GameLogDepth.Value * 2)
            .Reverse()
            .Select(e => (actor: e.Speaker, text: e.Content))
            .ToArray();

        if (allEntries.Length > 0) {
            var logPanel = this.MakeCard();
            logPanel.HeaderCard("em_ui_recent_action");

            var langWidth = Lang.isEN ? 15f : 18f;
            var max = allEntries.Max(a => a.actor.Length) / EMono.ui.canvasScaler.scaleFactor * langWidth;

            foreach (var (actor, text) in allEntries) {
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
                entry.Spacer(0).LayoutElement().flexibleWidth = 1f;
                entry.TopicPair(activity.Status.ToString(), $"{activity.TokensInput} + {activity.TokensOutput}");
            }
        }
    }
}