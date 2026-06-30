using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.Services;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Emmersive.API.ThirdParty;

public static class EmAi
{
    public static bool IsAvailable => ApiPoolSelector.Instance.HasAnyAvailableServices();

    public static async UniTask<string> SendAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var apiPool = ApiPoolSelector.Instance;
        if (!apiPool.TryGetNextAvailable(out var provider)) {
            throw new InvalidOperationException("No available AI service");
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
                return string.Empty;
            }

            activity.SetStatus(EmActivity.StatusType.Completed);
            return response.Content!;
        } catch (OperationCanceledException) {
            activity.SetStatus(EmActivity.StatusType.Timeout);
            throw;
        } catch {
            activity.SetStatus(EmActivity.StatusType.Failed);
            throw;
        }
    }
}