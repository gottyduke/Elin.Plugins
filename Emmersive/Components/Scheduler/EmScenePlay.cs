using System;
using System.Net;
using System.Threading;
using Cwl.API.Attributes;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using Emmersive.API.Services;
using Emmersive.API.Services.SceneDirector;
using Emmersive.Contexts;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ReflexCLI.Attributes;

namespace Emmersive.Components;

public partial class EmScheduler
{
    public static void RequestScenePlay(ContextBuilder contextBuilder)
    {
        EmMod.Log<EmScheduler>("scene play scheduled");

        var ctx = contextBuilder.Build();
        ScenePlayAsync(ctx.ToHistory()).Forget(ExceptionProfile.DefaultExceptionHandler);
    }

    [ConsoleCommand("trigger_current")]
    internal static void TestCurrentZone()
    {
        var builder = ContextBuilder
            .CreateStandard()
            .Add(new NearbyCharaContext(EClass.pc));

        RequestScenePlay(builder);
    }

    internal static void RequestScenePlayWithTrigger()
    {
        var builder = ContextBuilder
            .CreateStandard()
            .Add(new NearbyCharaContext(EClass.pc))
            .Add(new SceneTriggerContext(_buffer));

        RequestScenePlay(builder);
    }

    internal static async UniTask ScenePlayAsync(ChatHistory context, int retries = -1)
    {
        if (retries < 0) {
            retries = EmConfig.Policy.Retries.Value;
        }

        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();

        var apiPool = ApiPoolSelector.Instance;
        if (!apiPool.TryGetNextAvailable(out var provider)) {
            return;
        }

        using var _ = EmActivity.StartNew(provider.Id);

        _sceneCts?.Dispose();
        _sceneCts = new();

        var timeout = EmConfig.Policy.Timeout.Value;
        var timeoutCts = UniTasklet.Timeout(timeout);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _sceneCts.Token);

        EmMod.DebugPopup<EmScheduler>("requesting...");

        SetScenePlayDelay(timeout);

        try {
            var response = await provider.HandleRequest(kernel, context, cts.Token);

            if (response.Content.IsEmpty()) {
                EmMod.WarnWithPopup<EmScheduler>($"empty response received from [{provider.Id}");
                return;
            }

            var director = kernel.GetRequiredService<SceneDirector>();
            director.Execute(response.Content!);

            EmMod.Log($"finished\n{response}");

            pc.Profile.SetTalked();
        } catch (OperationCanceledException) {
            MarkUnavailable($"timeout after {EmConfig.Policy.Timeout.Value}s");
        } catch (HttpOperationException httpEx) when (httpEx.StatusCode != HttpStatusCode.BadRequest) {
            MarkUnavailable($"request failed: {httpEx.StatusCode!.TagColor(0xff0000)}\\{httpEx.Message}");

            if (retries == 0) {
                EmMod.Debug<EmScheduler>("no more retries");
                return;
            }

            ScenePlayAsync(context, --retries).Forget(ExceptionProfile.DefaultExceptionHandler);
        } catch (Exception ex) {
            MarkUnavailable($"request failed\n{ex.GetType().Name}\n{ex.Message}");

            throw;
        } finally {
            CoroutineHelper.Deferred(Cleanup, () => cts.IsCancellationRequested);
        }

        return;

        void Cleanup()
        {
            timeoutCts.Dispose();
            _sceneCts?.Dispose();
            cts.Dispose();
        }

        void MarkUnavailable(string message)
        {
            EmMod.Warn<EmScheduler>(message);
            provider.MarkUnavailable(message);
        }
    }

    [CwlSceneInitEvent(Scene.Mode.Title)]
    private static void OnSceneExit()
    {
        _sceneCts?.Cancel();
        _sceneCts?.Dispose();
    }
}