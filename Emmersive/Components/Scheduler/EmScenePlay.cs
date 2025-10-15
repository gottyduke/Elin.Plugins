using System;
using System.Net;
using System.Threading;
using Cwl.API.Attributes;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
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

    public static void RequestScenePlay(ContextBuilder contextBuilder)
    {
        ScenePlayAsync(contextBuilder).Forget(ExceptionProfile.DefaultExceptionHandler);
    }

    internal static async UniTask ScenePlayAsync(ContextBuilder contextBuilder, int retries = -1)
    {
        await UniTask.SwitchToThreadPool();
        var context = contextBuilder.Build().ToHistory();
        await ScenePlayAsync(context, retries);
    }

    internal static async UniTask ScenePlayAsync(ChatHistory context, int retries = -1)
    {
        if (Mode == ScheduleMode.Stop) {
            return;
        }

        if (retries < 0) {
            retries = EmConfig.Policy.Retries.Value;
        }

        EmMod.Log<EmScheduler>("em_ui_scene_scheduled".Loc(retries));

        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();

        var apiPool = ApiPoolSelector.Instance;
        if (!apiPool.TryGetNextAvailable(out var provider)) {
            return;
        }

        using var activity = EmActivity.StartNew(provider.Id);

        _sceneCts?.Dispose();
        _sceneCts = new();

        var timeout = EmConfig.Policy.Timeout.Value;
        var timeoutCts = UniTasklet.Timeout(timeout);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _sceneCts.Token);

        EmMod.DebugPopup<EmScheduler>("em_ui_scene_requesting".Loc());

        SetScenePlayDelay(timeout);

        try {
            var response = await provider.HandleRequest(kernel, context, cts.Token);

            var director = kernel.GetRequiredService<SceneDirector>();
            director.Execute(response.Content!);

            EmMod.Log("em_ui_scene_complete".Loc(response));

            pc.Profile.SetTalked();

            activity.Status = EmActivity.StatusType.Completed;
        } catch (OperationCanceledException) {
            MarkUnavailable("em_ui_scene_timeout".Loc(EmConfig.Policy.Timeout.Value));
        } catch (HttpOperationException httpEx) when (httpEx.StatusCode != HttpStatusCode.BadRequest) {
            MarkUnavailable("em_ui_scene_failed".Loc(httpEx.StatusCode!.TagColor(0xff0000), httpEx.Message));

            if (retries == 0) {
                EmMod.Debug<EmScheduler>("em_ui_scene_retry_end".Loc());
                return;
            }

            ScenePlayAsync(context, --retries).Forget(ExceptionProfile.DefaultExceptionHandler);
        } catch (Exception ex) {
            MarkUnavailable("em_ui_scene_failed".Loc(ex.GetType().Name, ex.Message));

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
            activity.Status = EmActivity.StatusType.Failed;
        }
    }

    [CwlSceneInitEvent(Scene.Mode.Title)]
    private static void OnSceneExit()
    {
        _sceneCts?.Cancel();
        _sceneCts?.Dispose();
    }
}