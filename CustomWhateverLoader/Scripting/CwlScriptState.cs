using System.Collections.Generic;
using System.Linq;
using Cwl.API.Drama;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Microsoft.CodeAnalysis.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    private static readonly Dictionary<string, CwlScriptState> _scriptStates = [];
    private static Stack<string> _activeStates = [];

    /// <summary>
    ///     Clear a script state and reset its variables
    /// </summary>
    [ConsoleCommand("state.remove")]
    public static void RemoveState(string state)
    {
        if (_scriptStates.Remove(state)) {
            CwlMod.Popup<ScriptState>("cwl_ui_cs_state_remove".lang());
        }

        _activeStates = new(_activeStates
            .Where(s => s != state)
            .Reverse());
    }

    /// <summary>
    ///     Pin a script state and freeze its variables
    /// </summary>
    [ConsoleCommand("state.pin")]
    public static void PinState(string state, bool pinned = true)
    {
        if (_scriptStates.TryGetValue(state, out var scriptState)) {
            scriptState.Pinned = pinned;
        }
    }

    [ConsoleCommand("state.push")]
    public static string PushState(string state)
    {
        TestIfDramaScriptStateActive();

        _activeStates.Push(state);
        return FormatCurrentStates();
    }

    [ConsoleCommand("state.pop")]
    public static string PopState()
    {
        TestIfDramaScriptStateActive();

        _activeStates.TryPop(out _);
        return FormatCurrentStates();
    }

    [ConsoleCommand("state")]
    private static string FormatCurrentStates()
    {
        using var sb = StringBuilderPool.Get();

        sb.AppendLine($"script states: [{_activeStates.Count}]");

        foreach (var state in _activeStates) {
            sb.Append($"{state}");

            if (_scriptStates.TryGetValue(state, out var stateInfo)) {
                if (stateInfo.Pinned) {
                    sb.AppendLine(" [PINNED]");
                }
            } else {
                sb.AppendLine(" [UNINITIALIZED]");
            }
        }

        return sb.ToString();
    }

    private static void TestIfDramaScriptStateActive()
    {
        if (DramaExpansion.Cookie?.Dm is not null) {
            throw new ScriptStateFrozenException(DramaExpansion.DramaScriptState);
        }
    }
}