using System;
using System.Collections.Generic;
using Cwl.API.Attributes;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using Emmersive.API.Plugins.SceneScheduler;
using Emmersive.API.Services;
using Emmersive.Contexts;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Emmersive;

[ConsoleCommandClassCustomizer("em")]
internal class EmScheduler : EMono
{
    private static readonly List<SceneTriggerEvent> _buffer = [];

    private static float _bufferStartTime = -1f;
    private static bool _isBuffering;

    private void Update()
    {
        if (_isBuffering && Time.time - _bufferStartTime >= 0.05f) {
            FlushBuffer();
        }
    }

    public static void OnTalkTrigger(SceneTriggerEvent trigger)
    {
        _buffer.Add(trigger);

        if (_isBuffering) {
            return;
        }

        _isBuffering = true;
        _bufferStartTime = Time.time;
    }

    private static void FlushBuffer()
    {
        _isBuffering = false;

        if (_buffer.Count == 0) {
            return;
        }

        RequestScenePlayWithTrigger();

        _buffer.Clear();
    }

    private static void RequestScenePlayWithTrigger()
    {
        var builder = ContextBuilder
            .Create()
            .Add(new NearbyCharaContext(EClass.pc))
            .Add(new SceneTriggerContext(_buffer));

        RequestScenePlay(builder);
    }

    internal static void RequestScenePlay(ContextBuilder contextBuilder)
    {
        var ctx = contextBuilder.Build();
        ScenePlayAsync(ctx.ToHistory()).RunOnPool();
    }

    [ConsoleCommand("trigger_current")]
    [CwlContextMenu("trigger zone")]
    private static void TriggerCurrentZone()
    {
        var builder = ContextBuilder
            .Create()
            .Add(new NearbyCharaContext(EClass.pc));

        RequestScenePlay(builder);
    }

    internal static async UniTaskVoid ScenePlayAsync(ChatHistory context, int retries = 1)
    {
        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();

        var timeout = UniTasklet.Timeout(EmConfig.Policy.Timeout.Value);
        var apiPool = ApiPoolSelector.Instance;
        if (!apiPool.TryGetNextAvailable(out var provider)) {
            return;
        }

        ChatMessageContent response;

        try {
            response = await provider.HandleRequest(kernel, context, timeout.Token);
        } catch (OperationCanceledException) {
            EmMod.Warn<EmScheduler>($"request timeout after {EmConfig.Policy.Timeout.Value}s");
            return;
        } catch (HttpOperationException httpEx) {
            EmMod.Warn<EmScheduler>($"request failed: {httpEx.StatusCode}\\{httpEx.Message}");

            provider.MarkUnavailable(httpEx.StatusCode.ToString());

            if (retries == 0) {
                EmMod.Debug<EmScheduler>("no more retries");
                return;
            }

            ScenePlayAsync(context, --retries).Forget();
            return;
        } catch (Exception ex) {
            EmMod.Warn("request failed");

            apiPool.CurrentProvider?.MarkUnavailable(ex.Message);

            throw;
        } finally {
            CoroutineHelper.Deferred(timeout.Dispose, () => timeout.IsCancellationRequested);
        }

        EmMod.Log($"finished {response}");
    }
}