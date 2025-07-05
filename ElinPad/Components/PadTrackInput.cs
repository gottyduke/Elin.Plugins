using System.Text;
using Cwl.Helper.String;
using ElinPad.Implementation.Event;
using ElinPad.Native;
using UnityEngine;

namespace ElinPad.Components;

internal class PadTrackInput : EMono
{
    private const int TrackWindowId = unchecked((int)1145141919810);

    private readonly FastString _lastPadInfo = new(256);

    private Vector2 _lsAxes = Vector2.zero;
    private float _ltValue;
    private Vector2 _rsAxes = Vector2.zero;
    private float _rtValue;
    private Rect _trackInputRect = new(10, 10, 400, 400);
    private Texture2D? _triggerBarTex;

    public static Gamepad MainPad => PadController.MainPad;

    private void Awake()
    {
        PadEventManager.OnPadButtonEvent += HandlePadEvent;
        PadEventManager.OnPadAxisEvent += HandlePadEvent;
    }

    private void OnDestroy()
    {
        PadEventManager.OnPadButtonEvent -= HandlePadEvent;
        PadEventManager.OnPadAxisEvent -= HandlePadEvent;
    }

    private void OnGUI()
    {
        if (!MainPad.IsConnected || !EpConfig.ShowTrackInput) {
            return;
        }

        _trackInputRect = GUI.Window(TrackWindowId, _trackInputRect, DrawWindow, $"Controller {MainPad.Index}");
    }

    private void DrawWindow(int windowId)
    {
        var textStyle = new GUIStyle(GUI.skin.label) {
            wordWrap = false,
            alignment = TextAnchor.UpperCenter,
            padding = new(10, 10, 5, 5),
            fontSize = 22,
        };

        GUILayout.Label(_lastPadInfo.ToString(), textStyle);

        DrawControllerVisualization();

        GUI.DragWindow(new(0, 0, _trackInputRect.width, 20));
    }

    private void DrawControllerVisualization()
    {
        const int elementSize = 80;
        const int triggerHeight = 100;
        const int triggerWidth = 25;

        var labelStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
        };

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        {
            GUILayout.BeginVertical(GUILayout.Width(triggerWidth));
            GUILayout.Label("LT", labelStyle, GUILayout.Height(20));
            DrawTriggerBar(triggerWidth, triggerHeight, _ltValue, new(0.4f, 0.7f, 1f, 0.8f));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(elementSize));
            GUILayout.Label("LStick", labelStyle, GUILayout.Height(20));
            var area = GUILayoutUtility.GetRect(elementSize, elementSize);
            DrawStickCircle(area, _lsAxes, new(0.2f, 0.8f, 0.8f, 0.7f));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(elementSize));
            GUILayout.Label("RStick", labelStyle, GUILayout.Height(20));
            var area2 = GUILayoutUtility.GetRect(elementSize, elementSize);
            DrawStickCircle(area2, _rsAxes, new(1f, 0.8f, 0.2f, 0.7f));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(triggerWidth));
            GUILayout.Label("RT", labelStyle, GUILayout.Height(20));
            DrawTriggerBar(triggerWidth, triggerHeight, _rtValue, new(0.0f, 0.4f, 0.8f, 0.8f));
            GUILayout.EndVertical();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void DrawTriggerBar(int width, int height, float value, Color color)
    {
        if (_triggerBarTex == null) {
            _triggerBarTex = new(1, 1);
            _triggerBarTex.SetPixel(0, 0, Color.white);
            _triggerBarTex.Apply();
        }

        var area = GUILayoutUtility.GetRect(width, height);

        GUI.color = new(0.2f, 0.2f, 0.2f, 0.3f);
        GUI.DrawTexture(area, _triggerBarTex);

        var fillHeight = value * area.height;
        var fillRect = new Rect(area.x, area.y + area.height - fillHeight, area.width, fillHeight);

        GUI.color = color;
        GUI.DrawTexture(fillRect, _triggerBarTex);

        GUI.color = new(0.8f, 0.8f, 0.8f, 0.5f);
        GUI.DrawTexture(new(area.x, area.y, area.width, 1), _triggerBarTex);
        GUI.DrawTexture(new(area.x, area.yMax - 1, area.width, 1), _triggerBarTex);
        GUI.DrawTexture(new(area.x, area.y, 1, area.height), _triggerBarTex);
        GUI.DrawTexture(new(area.xMax - 1, area.y, 1, area.height), _triggerBarTex);

        GUI.color = Color.white;
    }

    private static void DrawStickCircle(Rect area, Vector2 stickPos, Color color)
    {
        const int dotSize = 12;

        GUI.color = Color.white;
        GUI.DrawTexture(area, CreateCircleTexture((int)area.width, Color.white));

        var centerX = area.x + area.width / 2f;
        var centerY = area.y + area.height / 2f;
        var radius = area.width / 2f - dotSize / 2f;

        var posX = centerX + stickPos.x * radius;
        var posY = centerY - stickPos.y * radius;

        GUI.color = color;
        GUI.DrawTexture(new(posX - dotSize / 2f, posY - dotSize / 2f, dotSize, dotSize), CreateCircleTexture(dotSize, color));

        GUI.color = Color.white;
    }

    private static Texture2D CreateCircleTexture(int size, Color color)
    {
        var tex = new Texture2D(size, size);
        var colors = new Color[size * size];

        var radius = size / 2f;
        var radiusSq = radius * radius;

        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                var dx = x - radius;
                var dy = y - radius;
                var distSq = dx * dx + dy * dy;

                colors[x + y * size] = distSq <= radiusSq ? color : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    public string GetDebugInfo(GamepadState pad)
    {
        const int defaultColor = 0xffffff;

        return new StringBuilder()
            // triggers
            .AppendColorEx($"[LT] <{_ltValue:F3}> ", () => _ltValue > 0f, 0x66b2ff, defaultColor)
            .AppendLineColorEx($"[RT] <{_rtValue:F3}>", () => _rtValue > 0f, 0x0066cc, defaultColor)
            // bumpers
            .AppendColorEx("[LB] ", () => pad.LeftBumper, 0xffbc4f, defaultColor)
            .AppendLineColorEx("[RB]", () => pad.RightBumper, 0xffa91c, defaultColor)
            // left stick
            .AppendColorEx("[LStick] ", () => pad.LeftStick, 0x20b2aa, defaultColor)
            .AppendColorEx($"<Hor {_lsAxes.x:F3}> ", () => _lsAxes.x != 0f, 0x48d1cc, defaultColor)
            .AppendLineColorEx($"<Ver {_lsAxes.y:F3}>", () => _lsAxes.y != 0f, 0x48d1cc, defaultColor)
            // right stick
            .AppendColorEx("[RStick] ", () => pad.RightStick, 0xffa500, defaultColor)
            .AppendColorEx($"<Hor {_rsAxes.x:F3}> ", () => _rsAxes.x != 0f, 0xffd700, defaultColor)
            .AppendLineColorEx($"<Ver {_rsAxes.y:F3}>", () => _rsAxes.y != 0f, 0xffd700, defaultColor)
            // direction pad
            .AppendColorEx("[D-Up] ", () => pad.DPadUp, 0xe066ff, defaultColor)
            .AppendColorEx("[D-Down] ", () => pad.DPadDown, 0xba55d3, defaultColor)
            .AppendColorEx("[D-Left] ", () => pad.DPadLeft, 0xba55d3, defaultColor)
            .AppendLineColorEx("[D-Right]", () => pad.DPadRight, 0xe964ff, defaultColor)
            // buttons
            .AppendColorEx("[X] ", () => pad.X, 0x40dced, defaultColor)
            .AppendColorEx("[Y] ", () => pad.Y, 0xffb200, defaultColor)
            .AppendColorEx("[A] ", () => pad.A, 0x32a852, defaultColor)
            .AppendLineColorEx("[B]", () => pad.B, 0xd43a37, defaultColor)
            // menus
            .AppendColorEx("[View] ", () => pad.View, 0x9370db, defaultColor)
            .AppendLineColorEx("[Menu]", () => pad.Menu, 0x794bd6, defaultColor)
            .ToString();
    }

    private void HandlePadEvent(object sender, PadEventArgs input)
    {
        var pad = input.Gamepad;

        _ltValue = pad.LeftTriggerAxis;
        _rtValue = pad.RightTriggerAxis;
        _lsAxes = pad.LeftStickAxes;
        _rsAxes = pad.RightStickAxes;

        _lastPadInfo.Set(GetDebugInfo(pad));
    }
}