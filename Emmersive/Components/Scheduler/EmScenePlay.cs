using System;
using System.Linq;
using System.Net;
using Cwl.Helper.Exceptions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
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
    [ConsoleCommand("test_current")]
    public static void RequestScenePlayImmediate(Chara? focus = null)
    {
        focus ??= EClass.pc;
        var builder = ContextBuilder
            .CreateStandardPrefix()
            .Add(new NearbyCharaContext(focus))
            .Add(new NearbyThingContext(focus))
            .Add(ContextBuilder.RecentActionContext);

        RequestScenePlayWithContext(builder);
    }

    public static void RequestScenePlayWithTrigger(Chara? focus = null)
    {
        focus ??= EClass.pc;
        var builder = ContextBuilder
            .CreateStandardPrefix()
            .Add(new NearbyCharaContext(focus))
            .Add(new NearbyThingContext(focus))
            .Add(ContextBuilder.RecentActionContext)
            .Add(new SceneTriggerContext(_buffer.Copy()));

        RequestScenePlayWithContext(builder);
    }

    public static void RequestScenePlayWithContext(ContextBuilder contextBuilder)
    {
        ScenePlayAsync(contextBuilder).ForgetEx();
    }

    internal static async UniTask ScenePlayAsync(ContextBuilder contextBuilder, int retries = -1)
    {
        await UniTask.SwitchToThreadPool();

        try {
            await Semaphore.WaitAsync(UniTasklet.SceneCts.Token);

            var context = contextBuilder.Build().ToHistory();
            await ScenePlayAsync(context, retries);
        } finally {
            await UniTask.Yield();

            Semaphore.Release();
        }
    }

    internal static async UniTask ScenePlayAsync(ChatHistory context, int retries = -1)
    {
        if (retries < 0) {
            retries = Math.Max(0, Math.Min(EmConfig.Policy.Retries.Value, ApiPoolSelector.Instance.Providers.Count));
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

        EmMod.DebugPopup<EmScheduler>("em_ui_scene_requesting".lang());

        var timeout = EmConfig.Policy.Timeout.Value;

        SetScenePlayDelay(timeout);

        var lockedCharas = PointScan.LastNearby
            .Copy()
            .Where(c => !c.Profile.LockedInRequest)
            .Concat([EClass.pc])
            .ToArray();

        FreezeCharas(true);

        try {
            var response = await provider.HandleRequest(kernel, context, UniTasklet.SceneCts.Token);

            if (response.Content.IsEmpty()) {
                activity.SetStatus(EmActivity.StatusType.Failed);
            } else {
                activity.SetStatus(EmActivity.StatusType.Completed);

                var director = kernel.GetRequiredService<SceneDirector>();
                director.Execute(response.Content!);

                // start global cooldown
                pc.Profile.ResetTalkCooldown();
                GlobalCooldown = EmConfig.Policy.GlobalRequestCooldown.Value;

                EmMod.Debug("em_ui_scene_complete".Loc(response));
            }
        } catch (AggregateException ex)
            when (ex.InnerException is SchedulerDryRunException) {
            SwitchMode(SchedulerMode.Buffer);
            activity.SetStatus(EmActivity.StatusType.Unknown);
            // noexcept
        } catch (OperationCanceledException) {
            MarkUnavailable("em_ui_scene_timeout".Loc(timeout));
            // noexcept
        } catch (HttpOperationException httpEx) when (httpEx.StatusCode != HttpStatusCode.BadRequest) {
            ScheduleRetry(httpEx);
            // noexcept
        } catch (AggregateException ex)
            when (ex.InnerException is HttpOperationException { StatusCode: not HttpStatusCode.BadRequest } httpEx) {
            ScheduleRetry(httpEx);
            // noexcept
        } catch (Exception ex) {
            if (ex is AggregateException { InnerException: { } inner }) {
                ex = inner;
            }

            var message = ex.Message;
            if (ex is HttpOperationException { ResponseContent: { } responseContent }) {
                message = responseContent;
            }

            MarkUnavailable("em_ui_scene_failed".Loc(ex.GetType().Name, message));
            DebugThrow.Void(ex);
            // noexcept
        } finally {
            FreezeCharas(false);
        }

        return;

        void FreezeCharas(bool freeze)
        {
            foreach (var chara in lockedCharas) {
                chara.Profile.LockedInRequest = freeze;
            }
        }

        void MarkUnavailable(string message)
        {
            EmMod.Warn<EmScheduler>(message);
            provider.MarkUnavailable(message);
            activity.SetStatus(EmActivity.StatusType.Failed);
        }

        void ScheduleRetry(HttpOperationException httpEx)
        {
            MarkUnavailable("em_ui_scene_failed".Loc(httpEx.StatusCode!.TagColor(0xff0000), httpEx.Message));

            if (retries > 0) {
                EmMod.Debug<EmScheduler>("em_ui_scene_retry".lang());
                ScenePlayAsyncInternal(context, --retries).ForgetEx();
                EmMod.DebugPopup<EmScheduler>("scene retry");
            } else {
                EmMod.Debug<EmScheduler>("em_ui_scene_retry_end".lang());
            }
        }
    }
}