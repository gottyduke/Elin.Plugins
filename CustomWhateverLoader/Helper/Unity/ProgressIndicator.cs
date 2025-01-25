using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Cwl.Helper.Unity;

public class ProgressIndicator : EMono
{
    private static readonly List<ProgressIndicator> _active = [];

    private ProgressUpdater? _updater;

    public PopItemText Pop => GetComponent<PopItemText>();

    private void Update()
    {
        if (_updater is null) {
            Destroy(this);
            return;
        }

        var (onUpdate, shouldKill, linger) = _updater;
        if (Pop == null) {
            return;
        }

        Sync(onUpdate());

        if (shouldKill()) {
            StartCoroutine(DeferredKill(linger, onUpdate));
        }
    }

    public static ProgressIndicator? CreateProgress(Func<UpdateInfo> onUpdate, Func<bool> shouldKill, float lingerDuration = 10f)
    {
        if (ui?.popSystem == null) {
            return null;
        }

        var pop = ui.popSystem.PopText("", id: "PopAchievement", duration: 999f);
        if (pop == null) {
            return null;
        }

        ui.popSystem.insert = false;

        pop.name = "PopProgress";
        pop.important = true;
        pop.text.alignment = TextAnchor.UpperLeft;
        pop.Rect().pivot = new(1f, 1f);

        var progress = pop.gameObject.GetOrAddComponent<ProgressIndicator>();
        progress._updater = new(onUpdate, shouldKill, lingerDuration);

        _active.Add(progress);
        return progress;
    }

    public static ScopeExit CreateProgressScoped(Func<UpdateInfo> onUpdate, float lingerDuration = 10f)
    {
        var scopedExit = new ScopeExit();
        CreateProgress(onUpdate, () => !scopedExit.Alive, lingerDuration);
        return scopedExit;
    }

    private IEnumerator DeferredKill(float linger, Func<UpdateInfo> onUpdate)
    {
        Sync(onUpdate());

        yield return new WaitForSecondsRealtime(linger);

        Pop.important = false;

        _updater = null;
        _active.Remove(this);
        ui.popSystem.Kill(Pop);
    }

    private void Sync(UpdateInfo info)
    {
        Pop.SetText(info.Text, info.Sprite, info.Color ?? default(Color));
        AdjustPosition();
    }

    private void AdjustPosition()
    {
        var rect = Pop.Rect();
        rect.DOComplete();

        var y = 0f;
        foreach (var progress in _active.OfType<ProgressIndicator>()) {
            if (progress == this) {
                rect.anchoredPosition = new(0f, y);
                break;
            }

            y -= progress.Pop.Rect().sizeDelta.y;
        }
    }

    public record UpdateInfo(string Text, Sprite? Sprite = null, Color? Color = null);

    private record ProgressUpdater(Func<UpdateInfo> OnUpdate, Func<bool> ShouldKill, float LingerDuration);
}