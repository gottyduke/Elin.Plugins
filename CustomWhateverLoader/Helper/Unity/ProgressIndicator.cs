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
        pop.SetText(update.Text, update.Sprite, update.Color ?? default(Color));

        if (!shouldKill()) {
            return;
        }

        StartCoroutine(DeferredKill(pop, onUpdate, linger));
    }

    public static ProgressIndicator? CreateProgress(Func<UpdateInfo> onUpdate, Func<bool> shouldKill, float lingerDuration = 10f)
    {
        var (text, sprite, color) = onUpdate();

        var pop = ui?.popSystem?.PopText(text, sprite, "PopAchievement", color ?? default(Color), duration: 1f);
        if (pop == null) {
            return null;
        }

        pop.important = true;
        pop.Rect().pivot = Vector2.one;

        var progress = pop.gameObject.GetOrAddComponent<ProgressIndicator>();
        progress._updater = new(pop, onUpdate, shouldKill, lingerDuration);

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
        var update = onUpdate();
        pop.SetText(update.Text, update.Sprite, update.Color ?? default(Color));

        yield return new WaitForSecondsRealtime(linger);

        pop.important = false;
        ui.popSystem.Kill(pop);
        _updater = null;
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