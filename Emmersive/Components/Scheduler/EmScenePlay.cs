using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using Cwl.API.Attributes;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using Emmersive.API.Exceptions;
using Emmersive.API.Plugins;
using Emmersive.API.Services;
using Emmersive.Contexts;
using Emmersive.Helper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ReflexCLI.Attributes;

namespace Emmersive.Components;

public partial class EmScheduler
{
    [field: AllowNull]
    private static CancellationTokenSource SceneCts
    {
        get => field ??= new();
        set;
    }

    [ConsoleCommand("test_current")]
    public static void RequestScenePlayImmediate()
    {
        var builder = ContextBuilder
            .CreateStandardPrefix()
            .Add(new NearbyCharaContext(EClass.pc));

        RequestScenePlayWithContext(builder);
    }

    public static void RequestScenePlayWithTrigger()
    {
        var builder = ContextBuilder
            .CreateStandardPrefix()
            .Add(new NearbyCharaContext(EClass.pc))
            .Add(new SceneTriggerContext(_buffer.Copy()));

        RequestScenePlayWithContext(builder);
    }

    public static void RequestScenePlayWithContext(ContextBuilder contextBuilder)
    {
        ScenePlayAsync(contextBuilder).Forget(ExceptionProfile.DefaultExceptionHandler);
    }

    internal static async UniTask ScenePlayAsync(ContextBuilder contextBuilder, int retries = -1)
    {
        await UniTask.SwitchToThreadPool();

        try {
            var context = contextBuilder.Build().ToHistory();
            await ScenePlayAsync(context, retries);
        } finally {
            await UniTask.Yield();
        }
    }

    internal static async UniTask ScenePlayAsync(ChatHistory context, int retries = -1)
    {
        if (retries < 0) {
            retries = Math.Max(0, EmConfig.Policy.Retries.Value);
        }

        EmMod.Log<EmScheduler>("em_ui_scene_scheduled".Loc(retries));

        await ScenePlayAsyncInternal(context, retries);
    }

    private static async UniTask ScenePlayAsyncInternal(ChatHistory context, int retries)
    {
        if (retries < 0) {
            return;
        }

        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();

        var apiPool = ApiPoolSelector.Instance;
        if (!apiPool.TryGetNextAvailable(out var provider)) {
            return;
        }

        using var activity = EmActivity.StartNew(provider.Id);

        EmMod.DebugPopup<EmScheduler>("em_ui_scene_requesting".Loc());

        var timeout = EmConfig.Policy.Timeout.Value;

        SetScenePlayDelay(timeout);

        var lockedCharas = PointScan.LastNearby
            .Copy()
            .Where(c => !c.Profile.LockedInRequest)
            .Concat([EClass.pc])
            .ToArray();

        FreezeCharas(true);

        try {
            var response = await provider.HandleRequest(kernel, context, SceneCts.Token);

            activity.SetStatus(EmActivity.StatusType.Completed);

            var director = kernel.GetRequiredService<SceneDirector>();
            director.Execute(response.Content!);

            pc.Profile.ResetTalkCooldown();

            EmMod.Debug("em_ui_scene_complete".Loc(response));
        } catch (SchedulerDryRunException) {
            // noexcept
        } catch (OperationCanceledException) {
            MarkUnavailable("em_ui_scene_timeout".Loc(timeout));
            // noexcept
        } catch (HttpOperationException httpEx) when (httpEx.StatusCode != HttpStatusCode.BadRequest) {
            MarkUnavailable("em_ui_scene_failed".Loc(httpEx.StatusCode!.TagColor(0xff0000), httpEx.Message));

            if (retries > 0) {
                EmMod.Debug<EmScheduler>("em_ui_scene_retry".Loc());
                await ScenePlayAsyncInternal(context, --retries);
            } else {
                EmMod.Debug<EmScheduler>("em_ui_scene_retry_end".Loc());
            }
            // noexcept
        } catch (Exception ex) {
            MarkUnavailable("em_ui_scene_failed".Loc(ex.GetType().Name, ex.Message));
            DebugThrow.Void(ex);
            // noexcept
        } finally {
            FreezeCharas(false);
        }

        return;

        void FreezeCharas(bool block)
        {
            foreach (var chara in lockedCharas) {
                chara.Profile.LockedInRequest = block;
            }
        }

        void MarkUnavailable(string message)
        {
            EmMod.Warn<EmScheduler>(message);
            provider.MarkUnavailable(message);
            activity.SetStatus(EmActivity.StatusType.Failed);
        }
    }

    [CwlSceneInitEvent(Scene.Mode.Title)]
    private static void OnSceneExit()
    {
        SceneCts.Cancel();
        SceneCts.Dispose();
        SceneCts = null!;
    }
}