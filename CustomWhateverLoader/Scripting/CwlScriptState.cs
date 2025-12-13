using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Drama;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using ReflexCLI.Attributes;

namespace Cwl.Scripting;

public partial class CwlScriptLoader
{
    private static readonly Dictionary<string, CwlScriptState> _scriptStates = [];
    private static Stack<string> _activeStates = [];

    public static string? ActiveState => _activeStates.TryPeek(out var state) ? state : null;

    /// <summary>
    ///     Clear a script state and reset its variables
    /// </summary>
    [ConsoleCommand("state.remove")]
    public static void RemoveState(string state)
    {
        _scriptStates.Remove(state);
        _activeStates = new(_activeStates
            .Where(s => s != state)
            .Reverse());
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
            sb.Append(state);
            sb.AppendLine(_scriptStates.TryGetValue(state, out var scriptState)
                ? $" [{scriptState.Variables.Count}]"
                : " [UNINITIALIZED]");
        }

        return sb.ToString();
    }

    private static void TestIfDramaScriptStateActive()
    {
        if (DramaExpansion.Cookie?.Dm is not null) {
            throw new ScriptStateFrozenException(DramaExpansion.DramaScriptState);
        }
    }

    public class CwlScriptState
    {
        // hold a reference to the csharp package
        internal readonly Dictionary<string, object?> Variables = new(StringComparer.Ordinal);

        public CwlScriptState Script => this;

        public object? this[string name]
        {
            get => Variables.GetValueOrDefault(name);
            set => Variables[name] = value;
        }
    }
}