using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cwl.Helper.String;
using Cwl.Helper.Stubs;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using ReflexCLI.Attributes;

namespace Cwl.Helper;

[ConsoleCommandClassCustomizer("cwl.stub")]
public class DebugSampler : MethodStub
{
    private static readonly HashSet<MethodStubHelper> _stubs = [];
    private static readonly Stopwatch _sw = Stopwatch.StartNew();
    private static readonly FastString _lastSamplerInfo = new(1024);
    private static MethodInfo? _lastAttached;
    private static ProgressIndicator? _samplerProgress;
    private static bool _killSamplerProgress;
    private static int _keepCount;

    private readonly long[] _buffer = new long[100000];

    private bool _full;
    private long _max = long.MinValue;
    private long _min = long.MaxValue;
    private int _pos;

    public double Average => FrameCount == 0 ? 0d : (double)Total / FrameCount;
    public long LastFrame => _buffer[_pos - 1];
    public long MaxFrame => _max != long.MinValue ? _max : 0;
    public long MinFrame => _min != long.MaxValue ? _min : 0;
    public long FrameCount => _full ? _buffer.Length : _pos;
    public long Total { get; private set; }

    [ConsoleCommand("view")]
    public static string EnableSamplerView()
    {
        if (_stubs.Count == 0) {
            return "no stubs attached";
        }

        _killSamplerProgress = false;
        if (_samplerProgress == null) {
            _samplerProgress = ProgressIndicator.CreateProgress(
                () => new(GetSamplerInfo()),
                () => _killSamplerProgress,
                1f);
        }

        return "enabled stub info view";
    }

    [ConsoleCommand("hide")]
    public static string DisableSamplerView()
    {
        ClearSamplerInfo();

        _killSamplerProgress = true;
        _samplerProgress = null;

        return "disabled";
    }

    [ConsoleCommand("clear")]
    public static void ClearSamplerInfo(string specific = "*")
    {
        foreach (var helper in _stubs) {
            if (specific != "*" && helper.TargetMethod.Name != specific) {
                continue;
            }

            var sampler = (helper.Stub as DebugSampler)!;
            CwlMod.Debug<DebugSampler>($"cleared {helper.Name} with {sampler.FrameCount} frames");

            sampler.Clear();
        }
    }

    [ConsoleCommand("spawn")]
    public static void TestSpawn(int keepCount = 0)
    {
        _keepCount = keepCount;

        var diff = _keepCount - EClass._map.charas.Count;
        if (diff <= 0) {
            return;
        }

        for (var i = 0; i < diff; ++i) {
            var chara = CharaGen.Create(EMono.sources.charas.map.Keys.RandomItem(), EClass.rnd(100));
            EClass._zone.AddCard(chara, EClass.pc.pos.GetNearestPoint(false, false, false, true));
        }
    }

    [ConsoleCommand("attach")]
    public static void AttachDebugSampler(string typeName, string methodName = "*", bool nested = false)
    {
        var type = TypeQualifier.GlobalResolve(typeName);
        if (type is null) {
            CwlMod.Debug<DebugSampler>($"could not find type {typeName}");
            return;
        }

        foreach (var method in type.GetCachedMethods()) {
            if (methodName != "*" && method.Name != methodName) {
                continue;
            }

            var helpers = nested
                ? MethodStubHelper.CreateNestedStubs(method, () => new DebugSampler())
                : [MethodStubHelper.CreateStub(method, new DebugSampler())];
            foreach (var info in helpers) {
                info.Enable();
                _stubs.Add(info);
            }

            _lastAttached = method;
        }

        EnableSamplerView();
    }

    [ConsoleCommand("detach")]
    public static void DetachDebugSampler(string specific = "*")
    {
        foreach (var info in _stubs.ToArray()) {
            if (specific != "*" && info.Name != specific) {
                continue;
            }

            info.Disable();
            _stubs.Remove(info);
        }

        _lastAttached = null;

        ClearSamplerInfo();
    }

    [ConsoleCommand("dump")]
    public static string DumpInfoChart()
    {
        var filtered = _stubs
            .OrderByDescending(s => (s.Stub as DebugSampler)!.Average)
            .ToArray();

        if (filtered.Length == 0) {
            return "empty sequence";
        }

        var sb = new StringBuilder()
            .AppendLine("method,counted,average,max,min,total");

        foreach (var helper in filtered) {
            var sampler = (helper.Stub as DebugSampler)!;
            sb.AppendLine(
                $"{helper.Name},{sampler.FrameCount},{sampler.Average:F4},{sampler.MaxFrame},{sampler.MinFrame},{sampler.Total}");
        }

        var dump = $"{CorePath.rootExe}/stub_perf_{DateTime.Now:MM_dd_hh_mm_ss}.csv";
        File.WriteAllText(dump, sb.ToString());

        return $"output has been dumped to {dump.NormalizePath()}";
    }

    private static string GetSamplerInfo()
    {
        return _lastSamplerInfo
            .Watch(WatchSamplerInfo, BuildSamplerInfo)
            .With($"{EClass._map.charas.Count} charas");
    }

    private static object WatchSamplerInfo()
    {
        return _stubs.Sum(s => (s.Stub as DebugSampler)!.Total);
    }

    private static string BuildSamplerInfo()
    {
        var sb = new StringBuilder()
            .AppendLine("cwl_ui_stub_info".Loc())
            .AppendLine("cwl_ui_stub_header".Loc())
            .AppendLine()
            .AppendLine(_lastAttached!.GetAssemblyDetailColor(false));

        var tally = (long)WatchSamplerInfo();
        if (tally == 0) {
            sb.AppendLine("empty sequence o_O");
        } else {
            var filtered = _stubs
                .Select(s => new {
                    s.Name,
                    Average = (s.Stub as DebugSampler)!.Average / TimeSpan.TicksPerMillisecond,
                    Percentage = (double)(s.Stub as DebugSampler)!.Total / tally,
                })
                .OrderByDescending(s => s.Percentage)
                .Take(20)
                .ToArray();

            foreach (var entry in filtered) {
                sb.AppendLine($"{entry.Percentage:00.00%} / {entry.Average:F4}  {entry.Name}");
            }
        }

        sb.AppendLine();

        if (_keepCount != 0) {
            TestSpawn(_keepCount);
        }

        return sb.ToString();
    }

    public override void OnEnable()
    {
        Clear();
    }

    public override void OnDisable()
    {
        Clear();
    }

    public override void Begin()
    {
        var start = _sw.ElapsedTicks;
        if (_full) {
            Total -= _buffer[_pos];
        }

        _buffer[_pos] = start;
    }

    public override void End()
    {
        var duration = _sw.ElapsedTicks - _buffer[_pos];

        _buffer[_pos] = duration;
        Total += duration;
        _max = Math.Max(_max, duration);
        _min = Math.Min(_min, duration);

        if (++_pos < _buffer.Length) {
            return;
        }

        _full = true;
        _pos = 0;
    }

    private void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);

        _full = false;
        _pos = 0;
        Total = 0L;
        _max = long.MinValue;
        _min = long.MaxValue;
    }
}