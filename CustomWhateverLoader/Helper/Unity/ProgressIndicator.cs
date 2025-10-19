using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cwl.Helper.Unity;

public class ProgressIndicator
{
    private const float UpdateInterval = 0.2f;

    private static readonly List<ProgressIndicator> _active = [];

    private readonly string _hoverClosePrompt = "cwl_ui_hover_close".lang();
    private readonly string _hoverDetailPrompt = "cwl_ui_hover_detail".lang();

    private UpdateInfo? _info;
    private Action<ProgressIndicator>? _onAfterGUI;
    private Action<ProgressIndicator>? _onBeforeGUI;
    private Action<ProgressIndicator, Event>? _onEvent;
    private Action<ProgressIndicator>? _onHover;
    private Action? _onKill;
    public required Func<UpdateInfo> OnUpdate;
    public required Func<ProgressIndicator, bool> ShouldKill;

    [field: AllowNull]
    private static ProgressIndicatorUpdater Updater => field ??= CwlMod.Instance.GetOrCreate<ProgressIndicatorUpdater>();

    [field: AllowNull]
    public GUIStyle GUIStyle => field ??= GetLabelSkin();

    public float DurationRemain { get; private set; }
    public float DurationTotal { get; private set; }
    public bool IsHovering { get; private set; }
    public bool IsKilled { get; private set; }

    /// <summary>
    ///     Rect of current progress gui drawn.<br />
    ///     Use this as max size reference in OnAfterGUI
    /// </summary>
    public Rect Rect { get; private set; }

    public void Kill()
    {
        if (IsKilled) {
            return;
        }

        _active.Remove(this);
        _onKill?.Invoke();

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
            ExceptionProfile.GetFromStackTrace(ref ex).Analyze();
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

        var textColor = _info.Color ?? Color.black;
        GUIStyle.normal.textColor = GUIStyle.hover.textColor = textColor;

        GUILayout.BeginVertical(GUI.skin.box);
        {
            _onBeforeGUI?.Invoke(this);

            if (_info.Texture != null) {
                var width = Mathf.Min(_info.Texture.width, 400f);
                var height = (float)_info.Texture.height / _info.Texture.width * width;
                GUILayout.Label(_info.Texture, GUIStyle, GUILayout.Width(width), GUILayout.Height(height));
            }

            GUILayout.Label(GetUpdateText(), GUIStyle);

            if (IsHovering) {
                _onHover?.Invoke(this);
            }

            _onAfterGUI?.Invoke(this);
        }
        GUILayout.EndVertical();

        HandleEvent(Event.current);
    }

    private void HandleEvent(Event eventData)
    {
        switch (eventData.type) {
            case EventType.Repaint:
                Rect = GUILayoutUtility.GetLastRect();
                IsHovering = Rect.Contains(eventData.mousePosition);
                Rect = Rect with { size = Rect.size - new Vector2(10f, 10f) };
                break;
            case EventType.KeyDown or EventType.MouseDown or EventType.ScrollWheel
                when IsHovering: {
                _onEvent?.Invoke(this, eventData);

                if (eventData.IsRightMouseDown) {
                    Kill();
                    eventData.Use();
                }

                EInput.Consume(true);
                break;
            }
            case EventType.Used when IsHovering: {
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

        Updater.StopOnNextUpdate = true;
    }

    public record UpdateInfo(string Text, Texture2D? Texture = null, Color? Color = null);

    private class ProgressIndicatorUpdater : MonoBehaviour
    {
        internal bool StopOnNextUpdate;

        private GUIStyle? ScrollViewStyle => field ??= GetScrollViewSkin();
        private GUIStyle? VerticalScrollbarStyle => field ??= GetVerticalScrollbarSkin();
        private GUIStyle? VerticalScrollbarThumbStyle => field ??= GetVerticalScrollbarThumbSkin();

        private void Start()
        {
            ProgressUpdateAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnGUI()
        {
            if (_active.Count == 0) {
                return;
            }

            GUI.skin.scrollView = ScrollViewStyle;
            GUI.skin.verticalScrollbar = VerticalScrollbarStyle;
            GUI.skin.verticalScrollbarThumb = VerticalScrollbarThumbStyle;

            // don't overlap with steam fps
            GUILayout.Space(15f);

            // maybe one day wrap this in a vertical layout group
            _active.ToArray().ForeachReverse(p => p.Draw());
        }

        private async UniTaskVoid ProgressUpdateAsync(CancellationToken token)
        {
            while (!StopOnNextUpdate) {
                foreach (var progress in _active.ToArray()) {
                    progress.Update();
                }

                await UniTask.Delay(200, cancellationToken: token);
            }
        }
    }

#region Skins

    public static GUIStyle GetLabelSkin()
    {
        return new() {
            fontSize = 16,
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            padding = new(10, 10, 10, 10),
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(1f, 1f, 1f, 0.95f)),
                textColor = Color.black,
            },
            hover = {
                background = SpriteCreator.GetSolidColorTexture(new(0.9f, 0.9f, 0.9f)),
                textColor = Color.black,
            },
        };
    }

    public static GUIStyle GetScrollViewSkin()
    {
        return new(GUI.skin.scrollView) {
            padding = new(0, 10, 0, 0),
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(1f, 1f, 1f, 0.95f)),
            },
        };
    }

    public static GUIStyle GetVerticalScrollbarSkin()
    {
        return new(GUI.skin.verticalScrollbar) {
            fixedWidth = 5f,
            border = new(0, 0, 0, 0),
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(0.8f, 0.8f, 0.8f)),
            },
            hover = {
                background = SpriteCreator.GetSolidColorTexture(new(0.7f, 0.7f, 0.7f)),
            },
            active = {
                background = SpriteCreator.GetSolidColorTexture(new(0.6f, 0.6f, 0.6f)),
            },
        };
    }

    public static GUIStyle GetVerticalScrollbarThumbSkin()
    {
        return new(GUI.skin.verticalScrollbarThumb) {
            border = new(0, 0, 0, 0),
            fixedWidth = 10f,
            normal = {
                background = SpriteCreator.GetSolidColorTexture(new(0.5f, 0.5f, 0.5f)),
            },
            hover = {
                background = SpriteCreator.GetSolidColorTexture(new(0.4f, 0.4f, 0.4f)),
            },
            active = {
                background = SpriteCreator.GetSolidColorTexture(new(0.3f, 0.3f, 0.3f)),
            },
        };
    }

#endregion

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

        _ = Updater;

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

    public ProgressIndicator OnHover(Action<ProgressIndicator>? onHover)
    {
        _onHover = onHover;
        return this;
    }

    // not thread safe
    public ProgressIndicator OnKill(Action? onKill)
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