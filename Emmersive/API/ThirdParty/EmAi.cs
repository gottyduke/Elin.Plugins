using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.Services;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Emmersive.API.ThirdParty;

public static class EmAi
{
    public static bool IsAvailable => ApiPoolSelector.Instance.HasAnyAvailableServices();

    public static IReadOnlyList<string> GetModels()
    {
        return ApiPoolSelector.Instance.Providers
            .Select(p => p.Id)
            .ToList();
    }

    public static async UniTask<RequestReport> SendWithReportAsync(
        string systemPrompt,
        string userMessage,
        string? providerId = null,
        CancellationToken ct = default)
    {
        var apiPool = ApiPoolSelector.Instance;
        IChatProvider? provider;

        if (providerId is not null) {
            provider = apiPool.Providers.FirstOrDefault(p => p.Id == providerId);
            if (provider is null) {
                return RequestReport.Fail($"Provider '{providerId}' not found.");
            }

            provider.UpdateAvailability();
            if (!provider.IsAvailable) {
                return RequestReport.Fail($"Provider '{providerId}' is currently unavailable.", providerId);
            }
        } else if (!apiPool.TryGetNextAvailable(out provider)) {
            return RequestReport.Fail(
                apiPool.Providers.Count == 0
                    ? "No AI providers registered. Add a service in the Emmersive panel."
                    : "All registered providers are currently unavailable (cooldown or misconfigured).");
        }

        var kernel = EmKernel.Kernel ?? EmKernel.RebuildKernel();

        ChatHistory history = [];
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userMessage);

        using var activity = EmActivity.StartNew(provider.Id);

        try {
            var response = await provider.HandleRequest(kernel, history, ct);

            if (string.IsNullOrEmpty(response.Content)) {
                activity.SetStatus(EmActivity.StatusType.Failed);
                provider.MarkUnavailable("Empty response from provider");
                return RequestReport.Fail("Provider returned an empty response.", provider.Id);
            }

            activity.SetStatus(EmActivity.StatusType.Completed);
            return RequestReport.Ok(response.Content, provider.Id, activity);
        } catch (OperationCanceledException) {
            activity.SetStatus(EmActivity.StatusType.Timeout);
            provider.MarkUnavailable("Request timed out");
            return RequestReport.Fail("Request timed out.", provider.Id);
        } catch (Exception ex) {
            activity.SetStatus(EmActivity.StatusType.Failed);
            provider.MarkUnavailable(ex.Message);
            return RequestReport.Fail($"Request failed: {ex.Message}", provider.Id);
        }
    }
}