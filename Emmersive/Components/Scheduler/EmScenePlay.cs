using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.API.Exceptions;
using Emmersive.API.Plugins;
using Emmersive.API.Services;
using Emmersive.API.ThirdParty;
using Emmersive.ChatProviders;
using Emmersive.Contexts;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using Emmersive.LangMod;
using EModding.Helper.Runtime.Exceptions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ReflexCLI.Attributes;
using UniTasklet = Emmersive.Helper.UniTasklet;

namespace Emmersive.Components;

public partial class EmScheduler
{
    private static DateTime _lastParsePopup = DateTime.MinValue;

    [ConsoleCommand("test_current")]
    public static void RequestScenePlayImmediate(Chara? focus = null)
    {
        if (!CheckSceneRequest(Mode == SchedulerMode.DryRun)) {
            RestoreFromDryRun();
            return;
        }

        focus ??= pc;
        var nearby = NearbyCharaContext.GetNearbyChara(focus);
        var builder = BuildSceneContext(focus, nearby, null);

        builder.IsManual = true;
        RequestScenePlayWithContext(builder);
    }

    internal static bool CheckSceneRequest(bool dryRun)
    {
        string? error = null;
        if (!core.IsGameStarted) {
            error = "em_ui_need_game";
        } else if (!dryRun && Mode == SchedulerMode.Stop) {
            error = "em_ui_scheduler_off";
        } else if (ApiPoolSelector.Instance.Providers.Count == 0) {
            error = "em_ui_no_service";
        } else if (!EmAi.IsAvailable) {
            error = "em_ui_all_cooling";
        } else if (IsBusy) {
            error = "em_ui_scene_requesting";
        }

        if (error is not null) {
            EmMod.Popup<EmScheduler>(error.lang());
        }

        return error is null;
    }

    internal static void RequestScenePlayWithTrigger(SceneTriggerEvent[] triggers, Chara? focus = null)
    {
        focus ??= pc;
        var nearby = NearbyCharaContext.GetNearbyChara(focus);
        var builder = BuildSceneContext(focus, nearby, triggers);
        builder.TriggerCharas = [..triggers.Select(t => t.Chara)];
        builder.ShownLines = [..triggers.Where(t => t.AlreadyShown && !t.IsPlayer).Select(t => t.Trigger)];

        RequestScenePlayWithContext(builder);
    }

    public static void RequestScenePlayWithContext(ContextBuilder contextBuilder)
    {
        ScenePlayAsync(contextBuilder).ForgetEx();
    }

    private static ContextBuilder BuildSceneContext(Chara focus, IReadOnlyList<Chara> nearby, SceneTriggerEvent[]? triggers)
    {
        var builder = ContextBuilder
            .CreateStandardPrefix()
            .Add(new NearbyCharaContext(focus))
            .Add(new NearbyThingContext(focus));

        if (EmConfig.Memory.Enabled.Value) {
            var excluded = triggers?
                .Select(t => t.Trigger)
                .Where(t => !t.IsEmptyOrNull)
                .ToHashSet();

            builder.Add(new MemoryContext([..nearby, pc], excluded));
        }

        builder.AddExternalProviders();

        builder.Add(ContextBuilder.RecentActionContext);

        if (triggers is { Length: > 0 }) {
            builder.Add(new SceneTriggerContext(triggers));
        }

        builder.NearbyCharas = nearby;
        return builder;
    }

    internal static async UniTask ScenePlayAsync(ContextBuilder contextBuilder, int retries = -1)
    {
        var dryRun = Mode == SchedulerMode.DryRun;
        var triggerCharas = contextBuilder.TriggerCharas;
        var nearbyCharas = contextBuilder.NearbyCharas;

        Chara[] locked = [];

        var semaphore = Semaphore;
        var acquired = false;

        try {
            await semaphore.WaitAsync(UniTasklet.SceneCts.Token).ConfigureAwait(false);
            acquired = true;

            if (contextBuilder.IsManual && !dryRun) {
                EmMod.Popup<EmScheduler>("em_ui_scene_sent".lang());
            }

            locked = [
                ..nearbyCharas
                    .Append(pc)
                    .Except(triggerCharas)
                    .Distinct(),
            ];

            foreach (var chara in locked) {
                chara.Profile.LockedInRequest = true;
            }

            await ScenePlayAsyncInternal(contextBuilder, retries);
        } finally {
            foreach (var chara in triggerCharas.Concat(locked)) {
                chara.Profile.LockedInRequest = false;
            }

            if (acquired) {
                semaphore.Release();
            }

            if (dryRun) {
                RestoreFromDryRun();
            }
        }
    }

    private static async UniTask ScenePlayAsyncInternal(ContextBuilder contextBuilder, int retries)
    {
        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();
        var apiPool = ApiPoolSelector.Instance;

        if (contextBuilder.Build() is not { } args) {
            EmMod.Debug<EmScheduler>("game not started, scene request dropped");
            return;
        }

        var context = args.ToHistory();

        if (retries < 0) {
            retries = Math.Max(0, Math.Min(EmConfig.Policy.Retries.Value, apiPool.Providers.Count - 1));
        }

        EmMod.Log<EmScheduler>($"scene play scheduled, retries={retries}");

        for (var attempt = 0; attempt <= retries; attempt++) {
            if (!apiPool.TryGetNextAvailable(out var provider)) {
                return;
            }

            if (await PlayOnceAsync(kernel, context, provider, contextBuilder.ShownLines) == PlayResult.Done) {
                return;
            }

            EmMod.Debug<EmScheduler>(attempt < retries
                ? "scene request retry"
                : "scene request has no more retries");
        }
    }

    private static async UniTask<PlayResult> PlayOnceAsync(Kernel kernel,
                                                           ChatHistory context,
                                                           IChatProvider provider,
                                                           IReadOnlyCollection<string>? shownLines)
    {
        using var activity = EmActivity.StartNew(provider.Id);

        var timeout = provider is ChatProviderBase chat ? chat.TimeoutSeconds : EmConfig.Policy.Timeout.Value;
        var token = UniTasklet.SceneCts.Token;
        var zone = _zone;

        SetScenePlayDelay(timeout);
        var played = false;
        var dryRun = false;

        string? raw = null;
        string? error = null;

        try {
            var response = await provider.HandleRequest(kernel, context, activity, false, token);
            await UniTask.SwitchToMainThread();
            raw = response.Content;

            if (!core.IsGameStarted || !ReferenceEquals(_zone, zone)) {
                error = "zone changed during request, scene dropped";
                EmMod.Debug<EmScheduler>(error);
                activity.SetStatus(EmActivity.StatusType.Completed);
                return PlayResult.Done;
            }

            if (response.Content.IsEmptyOrNull) {
                error = "empty response";
                activity.SetStatus(EmActivity.StatusType.Failed);
                EmMod.Warn<EmScheduler>($"[{provider.Id}] {error}");
                return PlayResult.Done;
            }

            activity.SetStatus(EmActivity.StatusType.Completed);

            var sceneEnd = SceneDirector.Instance.Execute(response.Content!, shownLines);
            played = true;

            pc.Profile.ResetTalkCooldown();
            GlobalCooldown = sceneEnd + EmConfig.Policy.GlobalRequestCooldown.Value;

            EmMod.Log($"scene received\n{response}");
            return PlayResult.Done;
        } catch (Exception ex) {
            await UniTask.SwitchToMainThread();

            var cause = RequestFailure.Unwrap(ex);
            error = cause.Message;
            return OnFailure(cause);
        } finally {
            if (!played) {
                SetScenePlayDelay(0f);
            }

            if (!dryRun) {
                EmEvent.RaiseSceneRequestEnd(new() {
                    Success = played,
                    ProviderId = provider.Id,
                    Error = error,
                    RawContent = raw,
                });
            }
        }

        PlayResult OnFailure(Exception ex)
        {
            switch (ex) {
                case SchedulerDryRunException:
                    dryRun = true;
                    activity.SetStatus(EmActivity.StatusType.Unknown);
                    return PlayResult.Done;
                case FormatException:
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    EmMod.Warn<EmScheduler>(ex.Message);

                    if (DateTime.UtcNow - _lastParsePopup > TimeSpan.FromSeconds(60)) {
                        _lastParsePopup = DateTime.UtcNow;
                        EmMod.WarnWithPopup<EmScheduler>("em_ui_scene_parse_failed".lang());
                    }

                    return PlayResult.Done;
                case OperationCanceledException when token.IsCancellationRequested:
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    return PlayResult.Done;
                case OperationCanceledException:
                    activity.SetStatus(EmActivity.StatusType.Timeout);
                    MarkUnavailable("em_ui_scene_timeout".Loc(timeout));
                    return PlayResult.Done;
                case HttpOperationException http:
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    MarkUnavailable(RequestFailure.Describe(http, provider));
                    return RequestFailure.IsRetryable(http) ? PlayResult.Retry : PlayResult.Done;
                default:
                    activity.SetStatus(EmActivity.StatusType.Failed);
                    MarkUnavailable(RequestFailure.Describe(ex, provider));
                    DebugThrow.Void(ex);
                    return PlayResult.Done;
            }
        }

        void MarkUnavailable(string reason)
        {
            error = reason;
            EmMod.Warn<EmScheduler>($"scene request failed: {provider.Id}\n{reason}");
            provider.MarkUnavailable(reason);
        }
    }

    private enum PlayResult
    {
        Done,
        Retry,
    }
}