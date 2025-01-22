using System;
using System.Collections;
using UnityEngine;

namespace Cwl.Helper.Unity;

public class ProgressIndicator : EMono
{
    private ProgressUpdater? _updater;

    private void Update()
    {
        if (_updater is null) {
            Destroy(this);
            return;
        }

        var (pop, onUpdate, shouldKill, linger) = _updater;
        if (pop == null) {
            return;
        }

        var update = onUpdate();
        Sync(pop, update);

        if (!shouldKill()) {
            return;
        }

        StartCoroutine(DeferredKill(pop, onUpdate, linger));
    }

    public static ProgressIndicator? CreateProgress(Func<UpdateInfo> onUpdate, Func<bool> shouldKill, float lingerDuration = 10f)
    {
        if (ui?.popSystem == null) {
            return null;
        }

        var (text, sprite, color) = onUpdate();

        var pop = ui.popSystem.PopText(text, sprite, "PopAchievement", color ?? default(Color), duration: 999f);
        if (pop == null) {
            return null;
        }

        pop.important = true;
        pop.Rect().SetPivot(0.75f, 0.2f);

        var progress = pop.gameObject.GetOrAddComponent<ProgressIndicator>();
        progress._updater = new(pop, onUpdate, shouldKill, lingerDuration);

        ui.popSystem.maxLines++;

        return progress;
    }

    public static KillOnScopeExit CreateProgressScoped(Func<UpdateInfo> onUpdate, float lingerDuration = 10f)
    {
        var scopedExit = new KillOnScopeExit();
        CreateProgress(onUpdate, () => !scopedExit.Alive, lingerDuration);
        return scopedExit;
    }

    private IEnumerator DeferredKill(PopItemText pop, Func<UpdateInfo> onUpdate, float linger)
    {
        Sync(pop, onUpdate());

        yield return new WaitForSecondsRealtime(linger);

        pop.important = false;
        _updater = null;

        ui.popSystem.maxLines--;
        ui.popSystem.Kill(pop);
    }

    private static void Sync(PopItemText pop, UpdateInfo info)
    {
        pop.SetText(info.Text, info.Sprite, info.Color ?? default(Color));
        pop.RebuildLayout(true);
    }

    public class KillOnScopeExit : IDisposable
    {
        public bool Alive { get; private set; }

        public void Dispose()
        {
            Alive = false;
        }
    }

    public record UpdateInfo(string Text, Sprite? Sprite = null, Color? Color = null);

    private record ProgressUpdater(PopItemText Pop, Func<UpdateInfo> OnUpdate, Func<bool> ShouldKill, float LingerDuration);
}