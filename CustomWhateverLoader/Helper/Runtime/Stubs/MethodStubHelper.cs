using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Helper.Stubs;

public class MethodStubHelper(MethodInfo targetMethod)
{
    private static readonly Dictionary<MethodInfo, MethodStubHelper> _cached = [];
    private static readonly Dictionary<MethodBase, Dictionary<string, MethodStubHelper>> _cachedVirtual = [];
    private static readonly MethodInfo _smiBegin = AccessTools.Method(typeof(MethodStubHelper), nameof(OnBeginStub));
    private static readonly MethodInfo _smiEnd = AccessTools.Method(typeof(MethodStubHelper), nameof(OnEndStub));
    private static readonly MethodInfo _smiNested = AccessTools.Method(typeof(MethodStubHelper), nameof(OnNestedStubIl));
    private static readonly HashSet<MethodStubHelper> _nestedStubs = [];
    private static readonly Harmony _harmony = new(ModInfo.Guid);

    public MethodInfo TargetMethod => targetMethod;
    public string Name { get; private init; } = "";
    public bool IsEnabled { get; internal set; }
    public bool IsVirtual { get; internal init; }
    public MethodStub? Stub { get; internal set; }

    [SwallowExceptions]
    public void Enable()
    {
        if (IsEnabled) {
            return;
        }

        Stub?.OnEnable();

        if (!IsVirtual) {
            _harmony.Patch(targetMethod, new(_smiBegin), finalizer: new(_smiEnd));
        }

        CwlMod.Debug<MethodStubHelper>($"added stub for {Name}");

        IsEnabled = true;
    }

    [SwallowExceptions]
    public void Disable()
    {
        if (!IsEnabled) {
            return;
        }

        Stub?.OnDisable();

        if (!IsVirtual) {
            _harmony.Unpatch(targetMethod, _smiBegin);
            _harmony.Unpatch(targetMethod, _smiEnd);
        }

        CwlMod.Debug<MethodStubHelper>($"removed stub for {Name}");

        IsEnabled = false;
    }

    public static MethodStubHelper CreateStub(MethodInfo targetMethod, MethodStub stub, string? alias = null)
    {
        if (!_cached.TryGetValue(targetMethod, out var helper)) {
            helper = _cached[targetMethod] = new(targetMethod) {
                Name = alias ?? targetMethod.GetDetail(false),
                Stub = stub,
            };
        }

        return helper;
    }

    public static IEnumerable<MethodStubHelper> CreateNestedStubs(MethodInfo targetMethod, Func<MethodStub> stub)
    {
        _nestedStubs.Clear();

        try {
            _harmony.Patch(targetMethod, transpiler: new(_smiNested));
        } catch (Exception ex) {
            CwlMod.Debug(ex);
        }

        foreach (var helper in _nestedStubs) {
            helper.Stub = stub();
            yield return helper;
        }
    }

    private static void OnBeginStub(MethodInfo __originalMethod)
    {
        if (_cached.TryGetValue(__originalMethod, out var data) && data.IsEnabled) {
            data.Stub?.Begin();
        }
    }

    private static void OnEndStub(MethodInfo __originalMethod)
    {
        if (_cached.TryGetValue(__originalMethod, out var data) && data.IsEnabled) {
            data.Stub?.End();
        }
    }

    private static IEnumerable<CodeInstruction> OnNestedStubIl(IEnumerable<CodeInstruction> instructions,
                                                               MethodBase methodBase)
    {
        if (_cachedVirtual.TryGetValue(methodBase, out var invalidated)) {
            foreach (var old in invalidated.Values) {
                old.Disable();
            }
        }

        var virtualStubs = _cachedVirtual[methodBase] = [];
        return new CodeMatcher(instructions)
            .MatchStartForward(new OpCodeContains(nameof(OpCodes.Call)))
            .Repeat(cm => {
                if (cm.Operand is not MethodInfo mi) {
                    return;
                }

                var constrained = false;
                if (cm.Pos > 0) {
                    // constrained
                    var prev = cm.InstructionAt(-1);
                    constrained = prev.opcode == OpCodes.Constrained;
                }

                if (constrained) {
                    cm.Advance(-1);
                }

                var vmiKey = $"{mi.GetDetail(false)}/{cm.Pos}";
                if (virtualStubs.TryGetValue(vmiKey, out var helper)) {
                    return;
                }

                try {
                    helper = new(mi) {
                        Name = vmiKey,
                        IsVirtual = true,
                    };

                    cm.InsertAndAdvance(
                        Transpilers.EmitDelegate(() => helper.Stub?.Begin()));

                    if (cm.Labels.Count > 0) {
                        cm.InstructionAt(-1).labels.AddRange(cm.Labels);
                        cm.Labels.Clear();
                    }

                    if (constrained) {
                        cm.Advance(1);
                    }

                    cm.Advance(1);
                    cm.Insert(
                        Transpilers.EmitDelegate(() => helper.Stub?.End()));

                    _nestedStubs.Add(helper);
                } catch {
                    return;
                } finally {
                    cm.Advance(1);
                }

                virtualStubs[vmiKey] = helper;
            })
            .InstructionEnumeration();
    }
}