using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.LangMod;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cwl.Helper.Unity;

[RequireComponent(typeof(PopItemText))]
public class ProgressIndicator : EMono, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly List<ProgressIndicator> _active = [];
    private Func<string>? _onHoverUpdate;
    private Func<string>? _onTailUpdate;
    private Outline? _outline;
    private float _remaining;
    private UIText? _tailText;

    private ProgressUpdater? _updater;

    public PopItemText Pop => GetComponent<PopItemText>();
    public bool Hovering { get; private set; }

    private void Start()
    {
        StartCoroutine(UpdateProgress(0.2f));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) {
            Kill();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovering = true;
        _outline!.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovering = false;
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
        progress.GetOrCreate<UICollider>();
        progress.AppendOutline();

        _active.Add(progress);
        return progress;
    }

    /// <summary>
    ///     extremely simple progress that kills itself on scope exit
    /// </summary>
    /// <param name="onUpdate">update func</param>
    /// <param name="lingerDuration">default 10 seconds linger duration</param>
    /// <returns>
    ///     <see cref="IDisposable" /> resource via using statement, <see cref="ProgressIndicator" /> can
    ///     be accessed through property <see cref="ScopeExit.Object" />
    /// </returns>
    public static ScopeExit CreateProgressScoped(Func<UpdateInfo> onUpdate, float lingerDuration = 10f)
    {
        var scopeExit = new ScopeExit();
        scopeExit.Object = CreateProgress(onUpdate, () => !scopeExit.Alive, lingerDuration);
        return scopeExit;
    }

    public ProgressIndicator AppendHoverText(Func<string> onHover)
    {
        _onHoverUpdate = onHover;
        return this;
    }

    public ProgressIndicator AppendTailText(Func<string> onUpdate)
    {
        if (_tailText == null) {
            var tail = new GameObject("TailText");
            tail.transform.SetParent(transform);

            _tailText = tail.transform.GetOrCreate<UIText>();
            _tailText.color = Color.black;
            _tailText.fontType = FontType.Balloon;
            _tailText.enabled = true;

            var csf = _tailText.GetOrCreate<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        _onTailUpdate = onUpdate;
        return this;
    }

    private IEnumerator UpdateProgress(float interval)
    {
        if (_updater is null) {
            yield break;
        }

        var (onUpdate, shouldKill, lingerTime) = _updater;
        _remaining = lingerTime;

        while (_remaining > 0f) {
            UpdatePopup(onUpdate);

            yield return new WaitForSeconds(interval);

            if (shouldKill()) {
                if (!Hovering) {
                    _remaining -= interval;
                }
            } else {
                _remaining = lingerTime;
            }
        }

        Kill();
    }

    private void UpdatePopup(Func<UpdateInfo> onUpdate)
    {
        try {
            var (text, sprite, color) = onUpdate();

            if (Hovering) {
                text += $"\n{"cwl_ui_hover_close".Loc()}";
            }

            if (_onHoverUpdate is not null) {
                text += Hovering
                    ? $"\n{_onHoverUpdate()}"
                    : $"\n{"cwl_ui_hover_detail".Loc()}";
            }

            Pop.SetText(text, sprite, color ?? default(Color));

            if (_tailText != null && _onTailUpdate is not null) {
                _tailText.SetText(_onTailUpdate());
            }

            Pop.RebuildLayout(true);
            AdjustPosition();
        } catch {
            // noexcept
        }
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

    private void AppendOutline()
    {
        _outline = GetComponentInChildren<Image>().GetOrCreate<Outline>();
        _outline.effectDistance = new(2f, 2f);
        _outline.effectColor = Color.black;
        _outline.enabled = false;
    }

    public record UpdateInfo(string Text, Sprite? Sprite = null, Color? Color = null);

    private record ProgressUpdater(Func<UpdateInfo> OnUpdate, Func<bool> ShouldKill, float LingerDuration);
}