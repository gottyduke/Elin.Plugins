using System;
using System.Collections;
using System.Collections.Generic;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.LangMod;
using UnityEngine;

namespace Cwl.Helper.Unity;

public class ProgressIndicator
{
    private const float UpdateInterval = 0.2f;

    private static readonly List<ProgressIndicator> _active = [];
    private static ProgressIndicatorUpdater? _updater;
    private static GUIStyle? _defaultStyle;

    private readonly string _hoverClosePrompt = "cwl_ui_hover_close".Loc();
    private readonly string _hoverDetailPrompt = "cwl_ui_hover_detail".Loc();

    private UpdateInfo? _info;
    private Action<ProgressIndicator>? _onAfterGUI;
    private Action<ProgressIndicator>? _onBeforeGUI;
    private Action<ProgressIndicator, Event>? _onEvent;
    private Action<ProgressIndicator>? _onHover;
    private Action<ProgressIndicator>? _onKill;

    public GUIStyle? GUIStyle;
    public required Func<UpdateInfo> OnUpdate;
    public required Func<ProgressIndicator, bool> ShouldKill;

    public float DurationRemain { get; private set; }
    public bool IsHovering { get; private set; }
    public bool IsKilled { get; private set; }
    public float DurationTotal { get; private set; }

    public void Kill()
    {
        if (IsKilled) {
            return;
        }

        _active.Remove(this);
        _onKill?.Invoke(this);

        IsKilled = true;
    }

    private void Update()
    {
        try {
            if (ShouldKill(this) && !IsHovering) {
                DurationRemain -= UpdateInterval;
                if (DurationRemain <= 0f) {
                    Kill();
                    return;
                }
            } else {
                DurationRemain = DurationTotal;
            }

            _info = OnUpdate();
        } catch (Exception ex) {
            ExceptionProfile.GetFromStackTrace(ref ex).StartAnalyzing();
#if DEBUG
            throw;
#else
            Kill();
            // noexcept
#endif
        }
    }

    private string GetUpdateText()
    {
        using var sb = StringBuilderPool.Get();
        sb.Append(_info!.Text);

        var hasHover = _onHover is not null;
        if (IsHovering) {
            sb.AppendLine();
            sb.Append(_hoverClosePrompt);
        } else if (hasHover) {
            sb.AppendLine();
            sb.Append(_hoverDetailPrompt);
        }

        return sb.ToString();
    }

    private void Draw()
    {
        if (_info is null) {
            return;
        }

        GUIStyle ??= _defaultStyle ??= new(GUI.skin.label) {
            fontSize = 16,
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            padding = new(10, 10, 10, 10),
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(1f, 1f, 1f)),
                textColor = Color.black,
            },
            hover = {
                background = SpriteCreator.GetSolidColorTexture(new(0.9f, 0.9f, 0.9f)),
                textColor = Color.black,
            },
        };

        var textColor = _info.Color ?? Color.black;
        GUIStyle.normal.textColor = GUIStyle.hover.textColor = textColor;

        var oldStyle = GUI.skin.label;
        GUI.skin.label = GUIStyle;

        GUILayout.BeginVertical("box");
        {
            _onBeforeGUI?.Invoke(this);

            if (_info.Texture != null) {
                var width = Mathf.Min(_info.Texture.width, 400f);
                var height = (float)_info.Texture.height / _info.Texture.width * width;
                GUILayout.Label(_info.Texture, GUILayout.Width(width), GUILayout.Height(height));
            }

            GUILayout.Label(GetUpdateText());

            if (IsHovering) {
                _onHover?.Invoke(this);
            }

            _onAfterGUI?.Invoke(this);
        }
        GUILayout.EndVertical();

        GUI.skin.label = oldStyle;

        var @event = Event.current;
        switch (@event.type) {
            case EventType.Repaint:
                IsHovering = GUILayoutUtility.GetLastRect().Contains(@event.mousePosition);
                break;
            case EventType.MouseDown or EventType.KeyDown when IsHovering: {
                _onEvent?.Invoke(this, @event);

                if (@event.IsRightMouseDown || @event.IsMiddleMouseDown) {
                    Kill();
                }

                @event.Use();
                EInput.Consume(true);
                break;
            }
        }
    }

    public static void KillAll()
    {
        foreach (var progress in _active) {
            progress.Kill();
        }

        _updater?.StopOnNextUpdate = true;
    }

    public record UpdateInfo(string Text, Texture2D? Texture = null, Color? Color = null);

    private class ProgressIndicatorUpdater : MonoBehaviour
    {
        internal bool StopOnNextUpdate;

        private void Start()
        {
            StartCoroutine(ProgressUpdate());
        }

        private void OnGUI()
        {
            if (_active.Count == 0) {
                return;
            }

            GUILayout.Space(10f);

            _active.ToArray().ForeachReverse(p => p.Draw());
        }

        private IEnumerator ProgressUpdate()
        {
            var interval = new WaitForSeconds(UpdateInterval);
            while (!StopOnNextUpdate) {
                foreach (var progress in _active.ToArray()) {
                    progress.Update();
                }

                yield return interval;
            }
        }
    }

#region Factory

    public static ProgressIndicator CreateProgress(Func<UpdateInfo> onUpdate,
                                                   Func<ProgressIndicator, bool> shouldKill,
                                                   float duration = 10f)
    {
        var progress = new ProgressIndicator {
            OnUpdate = onUpdate,
            ShouldKill = shouldKill,
        };

        duration = Mathf.Max(duration, 0f);
        progress.SetDuration(duration, duration);

        _active.Add(progress);
        _updater ??= CwlMod.Instance!.transform.GetOrCreate<ProgressIndicatorUpdater>();

        return progress;
    }

    public static ScopeExit CreateProgressScoped(Func<UpdateInfo> onUpdate,
                                                 float duration = 7.5f)
    {
        var scopeExit = new ScopeExit();
        scopeExit.Object = CreateProgress(onUpdate, _ => !scopeExit.Alive, duration);
        return scopeExit;
    }

#endregion

#region Setters

    public ProgressIndicator SetGUIStyle(GUIStyle? styleOverride)
    {
        GUIStyle = styleOverride;
        return this;
    }

    public ProgressIndicator OnHover(Action<ProgressIndicator>? onHover)
    {
        _onHover = onHover;
        return this;
    }

    public ProgressIndicator OnKill(Action<ProgressIndicator>? onKill)
    {
        _onKill = onKill;
        return this;
    }

    public ProgressIndicator OnEvent(Action<ProgressIndicator, Event>? onGuiEvent)
    {
        _onEvent = onGuiEvent;
        return this;
    }

    public ProgressIndicator OnAfterGUI(Action<ProgressIndicator>? onGui)
    {
        _onAfterGUI = onGui;
        return this;
    }

    public ProgressIndicator OnBeforeGUI(Action<ProgressIndicator>? onGui)
    {
        _onBeforeGUI = onGui;
        return this;
    }

    public ProgressIndicator ResetDuration()
    {
        DurationRemain = DurationTotal;
        return this;
    }

    public ProgressIndicator SetDuration(float remain, float total = -1f)
    {
        if (total > 0f) {
            DurationTotal = total;
        }

        DurationRemain = Mathf.Clamp(remain, 0f, DurationTotal);
        return this;
    }

#endregion
}