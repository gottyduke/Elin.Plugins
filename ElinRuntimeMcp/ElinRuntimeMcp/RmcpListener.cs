using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EModding;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReflexCLI;
using UnityEngine;
using STask = System.Threading.Tasks.Task;

namespace ElinRuntimeMcp;

internal sealed class RmcpListener : EMono
{
    internal const int PortStart = 27551;
    internal const int PortCount = 10;

    internal const string ScriptState = "dev";
    internal const string HelloEvent = "runtimemcp.hello";
    internal const string StateEvent = "runtimemcp.state";

    private readonly CancellationTokenSource _cts = new();
    private static readonly HashSet<string> _hotLoaded = [];
    private readonly int _pid = Process.GetCurrentProcess().Id;
    private readonly ConcurrentQueue<Request> _queue = [];
    private TcpListener? _listener;

    internal int Port { get; private set; }

    private void Awake()
    {
        for (var port = PortStart; port < PortStart + PortCount; port++) {
            var listener = new TcpListener(IPAddress.Loopback, port);
            try {
                listener.Start();
            } catch (SocketException) {
                continue;
            }

            _listener = listener;
            Port = port;
            break;
        }

        if (_listener is null) {
            RmcpMod.Log.LogWarning($"no free port in {PortStart}-{PortStart + PortCount - 1}");
            return;
        }

        RmcpMod.Log.LogInfo($"listening on 127.0.0.1:{Port}");
        STask.Run(AcceptLoop);
    }

    private void Update()
    {
        while (_queue.TryDequeue(out var request)) {
            Execute(request);
        }
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _listener?.Stop();
    }

    private sealed class Request(JObject msg)
    {
        public readonly JObject Msg = msg;
        public readonly TaskCompletionSource<JToken?> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

#region Socket threads

    private async STask AcceptLoop()
    {
        while (!_cts.IsCancellationRequested) {
            TcpClient client;
            try {
                client = await _listener!.AcceptTcpClientAsync();
            } catch when (_cts.IsCancellationRequested) {
                return;
            } catch (Exception ex) {
                RmcpMod.Log.LogWarning($"accept failed: {ex.Message}");
                await STask.Delay(500);
                continue;
            }

            _ = HandleClient(client);
        }
    }

    private async STask HandleClient(TcpClient client)
    {
        using (client) {
            client.NoDelay = true;
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            writer.NewLine = "\n";

            while (!_cts.IsCancellationRequested) {
                string? line;
                try {
                    line = await reader.ReadLineAsync();
                } catch {
                    break;
                }

                if (line is null) {
                    break;
                }

                if (line.Length == 0) {
                    continue;
                }

                if (line[0] != '{') {
                    break;
                }

                var sw = Stopwatch.StartNew();
                JObject response;
                JToken? id = null;
                try {
                    var msg = JObject.Parse(line);
                    id = msg["id"];

                    // "log" and "ping" touch no Unity API: answered here so they work while the main thread is stuck
                    JToken? result;
                    var op = msg["op"]?.Type == JTokenType.String ? msg.Value<string>("op") : null;
                    if (op == "log") {
                        var args = msg["args"] as JObject ?? new JObject();
                        result = RmcpLogBuffer.Read(
                            args.Value<long?>("after") ?? 0,
                            Math.Clamp(args.Value<int?>("limit") ?? RmcpLogBuffer.Capacity, 1, RmcpLogBuffer.Capacity));
                    } else if (op == "ping") {
                        result = new JObject { ["pid"] = _pid, ["port"] = Port };
                    } else {
                        var request = new Request(msg);
                        _queue.Enqueue(request);
                        result = await request.Tcs.Task;
                    }

                    response = new() {
                        ["id"] = id,
                        ["ok"] = true,
                        ["result"] = result,
                        ["ms"] = sw.ElapsedMilliseconds,
                    };
                } catch (Exception ex) {
                    response = new() {
                        ["id"] = id,
                        ["ok"] = false,
                        ["error"] = $"{ex.GetType().Name}: {ex.Message}",
                        ["trace"] = ex.StackTrace,
                        ["ms"] = sw.ElapsedMilliseconds,
                    };
                }

                try {
                    await writer.WriteLineAsync(response.ToString(Formatting.None));
                } catch {
                    break;
                    // noexcept
                }
            }
        }
    }

#endregion

#region Main thread

    private void Execute(Request request)
    {
        try {
            var op = request.Msg.Value<string>("op") ?? throw new ArgumentException("missing string 'op'");
            var args = request.Msg["args"] as JObject ?? new JObject();

            switch (op) {
                case "hello":
                    request.Tcs.SetResult(Hello());
                    break;
                case "state":
                    request.Tcs.SetResult(State());
                    break;
                case "command":
                    request.Tcs.SetResult(Require(args, "cmd").EvaluateAsCommand());
                    break;
                case "eval":
                    request.Tcs.SetResult(Eval(Require(args, "code")));
                    break;
                case "screenshot":
                    StartCoroutine(Screenshot(request, args));
                    break;
                case "load":
                    request.Tcs.SetResult(Load(Require(args, "source"), args.Value<string>("file") ?? "hotload.cs", args.Value<string>("name")));
                    break;
                case "unload":
                    request.Tcs.SetResult(Unload(Require(args, "name")));
                    break;
                default:
                    throw new ArgumentException($"unknown op '{op}'");
            }
        } catch (Exception ex) {
            request.Tcs.TrySetException(ex);
        }
    }

    private static string Require(JObject args, string key)
    {
        return args.Value<string>(key) ?? throw new ArgumentException($"missing arg '{key}'");
    }

    // what another mod hands us is serialized key by key and defensively: one Vector3 (self-referencing
    // "normalized") must not take hello down with it, or the whole instance vanishes from elin_instances
    private static readonly JsonSerializer _extraSerializer = JsonSerializer.Create(new() {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = [new UnityAsText()],
    });

    private static void AddExtras(JObject target, string eventId)
    {
        var extras = new Dictionary<string, object?>();
        BaseModManager.PublishEvent(eventId, extras);
        foreach (var (key, value) in extras) {
            if (target.ContainsKey(key)) {
                continue;
            }

            try {
                var writer = new ShallowWriter(6);
                _extraSerializer.Serialize(writer, value);
                target[key] = Sanitize(writer.Token ?? JValue.CreateNull());
            } catch (Exception ex) {
                target[key] = $"<unserializable {value!.GetType().Name}: {ex.Message}>";
            }
        }
    }

    // Unity types serialize into property soup (Vector3.normalized recurses forever, a GameObject drags in its scene): use their text
    private sealed class UnityAsText : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType.Namespace?.StartsWith("UnityEngine", StringComparison.Ordinal) == true;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }

    // a mod handing over a Chara or similar would otherwise serialize half the game
    private sealed class ShallowWriter(int maxDepth) : JTokenWriter
    {
        public override void WriteStartObject()
        {
            Check();
            base.WriteStartObject();
        }

        public override void WriteStartArray()
        {
            Check();
            base.WriteStartArray();
        }

        private void Check()
        {
            if (Top >= maxDepth) {
                throw new JsonSerializationException($"nested deeper than {maxDepth} levels, pass plain data");
            }
        }
    }

    // JSON.parse on the bridge side turns integers beyond 2^53 into neighbours (a SteamID quietly changes); send them as text
    private static JToken Sanitize(JToken token)
    {
        switch (token) {
            case JValue { Type: JTokenType.Integer } v:
                var text = Convert.ToString(v.Value, CultureInfo.InvariantCulture)!;
                return long.TryParse(text, out var n) && Math.Abs(n) <= 1L << 53 ? v : new JValue(text);
            case JContainer container:
                foreach (var child in container.Children().ToArray()) {
                    var clean = Sanitize(child is JProperty p ? p.Value : child);
                    if (child is JProperty prop) {
                        prop.Value = clean;
                    } else if (!ReferenceEquals(clean, child)) {
                        child.Replace(clean);
                    }
                }

                return token;
            default:
                return token;
        }
    }

    private JObject Hello()
    {
        var hello = new JObject {
            ["pid"] = _pid,
            ["port"] = Port,
            ["version"] = ModInfo.Version,
            ["gameVersion"] = core?.version.GetText(),
            ["gameStarted"] = core?.IsGameStarted ?? false,
            ["sceneMode"] = core?.scene?.mode.ToString(),
            ["scripting"] = EScript.IsScriptingAvailable,
        };
        AddExtras(hello, HelloEvent);
        return hello;
    }

    private static JObject State()
    {
        var state = new JObject {
            ["gameStarted"] = core.IsGameStarted,
            ["sceneMode"] = core.scene?.mode.ToString(),
        };

        if (core.IsGameStarted) {
            var zone = EClass._zone;
            state["zone"] = new JObject {
                ["name"] = zone.ZoneFullName,
                ["uid"] = zone.uid,
                ["isRegion"] = zone.IsRegion,
            };

            var pc = EClass.pc;
            state["pc"] = new JObject {
                ["uid"] = pc.uid,
                ["name"] = pc.Name,
                ["x"] = pc.pos.x,
                ["z"] = pc.pos.z,
                ["hp"] = pc.hp,
                ["maxHp"] = pc.MaxHP,
                ["isDead"] = pc.isDead,
                ["held"] = pc.held?.Name,
                ["heldUid"] = pc.held?.uid,
                ["ai"] = pc.ai?.GetType().Name,
            };
        }

        AddExtras(state, StateEvent);
        return state;
    }

    private static JToken? Eval(string code)
    {
        if (!EScript.IsScriptingAvailable) {
            throw new InvalidOperationException("scripting provider unavailable, is _ModdingKit loaded?");
        }

        var states = (Dictionary<string, EScriptState>)AccessTools.Field(typeof(EScriptState), "ScriptStates").GetValue(null);
        if (!states.ContainsKey(ScriptState)) {
            states[ScriptState] = new();
        }

        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        object? result;
        try {
            result = code.EvaluateAsCsharp(useState: ScriptState, useCache: true, throwOnError: true);
        } finally {
            SynchronizationContext.SetSynchronizationContext(context);
        }

        // the script has already run: whatever it returned must come back as a value, never as a conversion error
        // (char and IntPtr have no JSON type; longs beyond 2^53 and decimals lose digits in JSON.parse)
        return result switch {
            null => null,
            string s => s,
            bool or byte or sbyte or short or ushort or int or uint or float or double => new JValue(result),
            long l when Math.Abs(l) <= 1L << 53 => new JValue(l),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => result.ToString(),
        };
    }

    // Load and Unload follow EScriptLoader.TryLoadScript / TryUnloadScript step for step, except that they run the
    // EScriptLoad / EScriptUnload hooks themselves: EScriptLoader swallows a hook's exception into a log line that only
    // says "Exception has been thrown by the target of an invocation", so the caller would hear "loaded" for a dead patch.

    private static string Load(string source, string file, string? name)
    {
        name ??= $"hot_{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:HHmmssfff}";
        if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == name)) {
            throw new InvalidOperationException($"assembly '{name}' is already loaded and Mono cannot unload it, pick another name");
        }

        var bytes = EScript.ScriptProvider.CompileAssembly([(source, file)], name, true);
        var assembly = Assembly.Load(bytes);

        assembly.RegisterScript(name);
        _hotLoaded.Add(name);
        var (ran, errors) = RunHook(assembly, "EScriptLoad");
        ClassCache.assemblies.Add(assembly.FullName);
        CommandRegistry.assemblies.Add(assembly);

        if (errors.Length > 0) {
            throw new InvalidOperationException(
                $"'{name}' is loaded but its EScriptLoad threw, so the patch is probably not applied. " +
                $"Fix it and load again under a new name.\n{errors}");
        }

        return ran == 0
            ? $"{name}: loaded, no EScriptLoad() found so nothing ran (call new Harmony(\"some.id\").PatchAll(...) from a static EScriptLoad)"
            : $"{name}: loaded, ran EScriptLoad in {ran} type(s)";
    }

    private static string Unload(string name)
    {
        // only what came in through Load: unregistering a real mod would break its type lookups and console commands
        if (!_hotLoaded.Contains(name)) {
            throw new ArgumentException(
                $"'{name}' was not hot-loaded in this session. Hot-loaded: {(_hotLoaded.Count > 0 ? string.Join(", ", _hotLoaded) : "none")}");
        }

        var assembly = AppDomain.CurrentDomain.GetAssemblies().Last(a => a.GetName().Name == name);

        assembly.UnregisterScript();
        var (ran, errors) = RunHook(assembly, "EScriptUnload");
        ClassCache.assemblies.Remove(assembly.FullName);
        CommandRegistry.assemblies.Remove(assembly);

        if (errors.Length > 0) {
            throw new InvalidOperationException($"EScriptUnload of '{name}' threw, so the patch may still be applied:\n{errors}");
        }

        return ran == 0
            ? $"{name}: no EScriptUnload() found, nothing was undone; the assembly stays loaded until the game restarts"
            : $"{name}: ran EScriptUnload in {ran} type(s); the assembly stays loaded until the game restarts";
    }

    /// <summary>Invoke every static method with this name, collecting the real (inner) exception of each failure.</summary>
    private static (int ran, string errors) RunHook(Assembly assembly, string method)
    {
        var ran = 0;
        var errors = new StringBuilder();
        foreach (var hook in assembly.GetTypes().SelectMany(t => t.GetMethods(AccessTools.all)).Where(m => m.Name == method && m.IsStatic)) {
            ran++;
            try {
                hook.Invoke(null, null);
            } catch (Exception ex) {
                var inner = ex is TargetInvocationException { InnerException: { } e } ? e : ex;
                errors.AppendLine($"{hook.DeclaringType?.FullName}.{method}: {inner.GetType().Name}: {inner.Message}");
                errors.AppendLine(inner.StackTrace);
            }
        }

        return (ran, errors.ToString());
    }

    private static IEnumerator Screenshot(Request request, JObject args)
    {
        yield return new WaitForEndOfFrame();

        Texture2D? tex = null;
        Texture2D? scaled = null;
        try {
            tex = ScreenCapture.CaptureScreenshotAsTexture();
            var src = tex;

            // always re-rendered into RGB24, also at scale 1: the raw capture carries alpha, which makes the PNG
            // larger and half-transparent in viewers
            var scale = Mathf.Clamp(args.Value<float?>("scale") ?? 0.5f, 0.1f, 1f);
            {
                var w = Mathf.Max(1, Mathf.RoundToInt(tex.width * scale));
                var h = Mathf.Max(1, Mathf.RoundToInt(tex.height * scale));
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                var prev = RenderTexture.active;
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                scaled = new(w, h, TextureFormat.RGB24, false);
                scaled.ReadPixels(new(0, 0, w, h), 0, 0);
                scaled.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                src = scaled;
            }

            request.Tcs.SetResult(new JObject {
                ["png"] = Convert.ToBase64String(src.EncodeToPNG()),
                ["width"] = src.width,
                ["height"] = src.height,
            });
        } catch (Exception ex) {
            request.Tcs.TrySetException(ex);
        } finally {
            if (tex) {
                Destroy(tex);
            }

            if (scaled) {
                Destroy(scaled);
            }
        }
    }

#endregion
}
