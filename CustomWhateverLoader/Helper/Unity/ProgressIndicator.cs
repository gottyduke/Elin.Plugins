using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cwl.Helper.Unity;

[RequireComponent(typeof(PopItemText))]
public class ProgressIndicator : EMono, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly List<ProgressIndicator> _active = [];

    private bool _hoverable;
    private Func<string>? _onHover;
    private Outline? _outline;
    private ProgressUpdater? _updater;

    public PopItemText Pop => GetComponent<PopItemText>();
    public bool Expanded { get; private set; }

    private void Start()
    {
        StartCoroutine(Sync(0.2f));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) {
            Kill();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hoverable) {
            return;
        }

        Expanded = true;
        _outline!.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_hoverable) {
            return;
        }

        Expanded = false;
        _outline!.enabled = false;
    }

    /// <summary>
    ///     a wrapper of <see cref="PopItemText" /> that offers continuous updating and various controls
    /// </summary>
    /// <param name="onUpdate">update func</param>
    /// <param name="shouldKill">predicate to notify kill</param>
    /// <param name="lingerDuration">default 10 seconds linger duration</param>
    /// <returns>the created instance, null if failed</returns>
    [SwallowExceptions]
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
        ui.popSystem.maxLines = int.MaxValue;

        pop.name = "PopProgress";
        pop.important = true;
        pop.text.alignment = TextAnchor.UpperLeft;
        pop.Rect().pivot = new(1f, 1f);

        var progress = pop.GetOrCreate<ProgressIndicator>();
        progress._updater = new(onUpdate, shouldKill, Mathf.Max(lingerDuration, 0f));

        _active.Add(progress);
        return progress;
    }

    /// <summary>
    ///     extremely simple progress that kills itself on scope exit
    /// </summary>
    /// <param name="onUpdate">update func</param>
    /// <param name="lingerDuration">default 10 seconds linger duration</param>
    /// <returns>
    ///     <see cref="IDisposable" /> resource via using statement, underlying <see cref="ProgressIndicator" /> can
    ///     be accessed through property <see cref="ScopeExit.Object" />
    /// </returns>
    public static ScopeExit CreateProgressScoped(Func<UpdateInfo> onUpdate, float lingerDuration = 10f)
    {
        var scopeExit = new ScopeExit();
        scopeExit.Object = CreateProgress(onUpdate, () => !scopeExit.Alive, lingerDuration);
        return scopeExit;
    }

    public ProgressIndicator SetHover(Func<string>? onHover = null)
    {
        if (_hoverable) {
            return this;
        }

        _hoverable = true;
        _onHover = onHover;

        _outline = GetComponentInChildren<Image>().GetOrCreate<Outline>();
        _outline.effectDistance = new(2f, 2f);
        _outline.effectColor = Color.black;
        _outline.enabled = false;

        this.GetOrCreate<UICollider>();

        return this;
    }

    private IEnumerator Sync(float interval)
    {
        if (_updater is null) {
            yield break;
        }

        var (onUpdate, shouldKill, lingerTime) = _updater;

        while (!shouldKill()) {
            UpdatePopup(onUpdate);
            yield return new WaitForSeconds(interval);
        }

        while (lingerTime > 0f) {
            UpdatePopup(onUpdate);
            yield return new WaitForSeconds(interval);
            lingerTime -= interval;
        }

        Kill();
    }

    private void UpdatePopup(Func<UpdateInfo> onUpdate)
    {
        var info = onUpdate();
        var text = info.Text;

        if (_hoverable && _onHover is not null) {
            var append = Expanded ? _onHover() : "<Hover For More Information>";
            text += $"\b{append}";
        }

        Pop.SetText(text, info.Sprite, info.Color ?? default(Color));
        AdjustPosition();
    }

    private void AdjustPosition()
    {
        var rect = Pop.Rect();
        var y = 0f;
        foreach (var progress in _active.OfType<ProgressIndicator>()) {
            if (progress == this) {
                rect.anchoredPosition = new(0f, y);
                break;
            }

            y -= progress.Pop.Rect().sizeDelta.y;
        }
    }

    private void Kill()
    {
        Pop.important = false;
        _active.Remove(this);
        ui.popSystem.Kill(Pop);
    }

    public record UpdateInfo(string Text, Sprite? Sprite = null, Color? Color = null);

    private record ProgressUpdater(Func<UpdateInfo> OnUpdate, Func<bool> ShouldKill, float LingerDuration);
}