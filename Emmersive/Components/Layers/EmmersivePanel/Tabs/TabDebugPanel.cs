using System.Collections.Generic;
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
        var actions = Horizontal()
            .WithSpace(10);
        actions.Layout.childForceExpandWidth = true;

        actions.Button("em_ui_scheduler_dry".lang(), () => TabAiService.RequestTestScene(true));

        BuildRecentActions();
        BuildRecentRequests();
    }

    private void BuildRecentActions()
    {
        var allEntries = MemoryManager.Instance.AllStores
            .SelectMany(s => s.GetRecentStm(EmConfig.Memory.MaxStmInContext.Value))
            .OrderByDescending(e => e.Turn)
            .Take(EmConfig.Context.GameLogDepth.Value * 2)
            .Reverse()
            .Select(e => (actor: e.Speaker, text: e.Content))
            .ToArray();

        if (allEntries.Length == 0) {
            return;
        }

        var logPanel = this.MakeCard();
        logPanel.HeaderCard("em_ui_recent_action");

        var labels = new List<Text>();
        foreach (var (actor, text) in allEntries) {
            var pair = logPanel.TopicPair(actor, text);
            pair.text1.alignment = TextAnchor.UpperLeft;
            labels.Add(pair.text1);
        }

        var fontSize = labels[0].fontSize;
        var width = allEntries.Max(e => e.actor.Sum(c => c >= '\u2E80' ? 1f : 0.6f)) * fontSize;
        foreach (var label in labels) {
            label.GetOrCreate<LayoutElement>().preferredWidth = width;
        }
    }

    private void BuildRecentRequests()
    {
        var activities = EmActivity.Session
            .TakeLast(15)
            .Reverse()
            .ToArray();

        if (activities.Length == 0) {
            return;
        }

        var requestPanel = this.MakeCard();
        requestPanel.HeaderCard("em_ui_recent_requests");

        requestPanel.ShowActivityInfo("");

        foreach (var activity in activities) {
            var entry = requestPanel.Horizontal()
                .WithSpace(5);
            entry.Layout.childForceExpandWidth = true;

            entry.TopicPair(activity.RequestTime.ToLocalTime().ToLongTimeString(), activity.ServiceName);
            entry.Spacer(0).LayoutElement().flexibleWidth = 1f;
            entry.TopicPair(StatusText(activity.Status), $"{activity.TokensInput} + {activity.TokensOutput}");
        }

        return;

        static string StatusText(EmActivity.StatusType status)
        {
            return status switch {
                EmActivity.StatusType.Completed => "em_ui_status_completed",
                EmActivity.StatusType.Failed => "em_ui_status_failed",
                EmActivity.StatusType.Timeout => "em_ui_status_timeout",
                EmActivity.StatusType.InProgress => "em_ui_status_in_progress",
                _ => "em_ui_status_dry_run",
            };
        }
    }
}