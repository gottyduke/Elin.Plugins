using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Emmersive.ChatProviders;

public abstract class ChatProviderBase : IChatProvider
{
    private DateTime _cooldownUntil = DateTime.MinValue;
    protected static int ServiceCount => ++field;

    public abstract string Id { get; }
    public abstract IReadOnlyList<string> Models { get; }
    public abstract string CurrentModel { get; set; }
    public abstract PromptExecutionSettings ExecutionSettings { get; set; }
    public bool IsAvailable => DateTime.Now >= _cooldownUntil;

    public void Register(IKernelBuilder builder)
    {
        Register(builder, CurrentModel);
    }

    public virtual void MarkUnavailable(string? message = null)
    {
        _cooldownUntil = DateTime.Now + TimeSpan.FromSeconds(15f);
        EmMod.Warn($"[{Id}] temporarily unavailable: {message}");
    }

    public void UpdateAvailability()
    {
        if (DateTime.Now >= _cooldownUntil) {
            _cooldownUntil = DateTime.MinValue;
        }
    }

    public virtual async UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token)
    {
        var service = kernel.GetRequiredService<IChatCompletionService>(Id);
        return await service.GetChatMessageContentAsync(context, ExecutionSettings, kernel, token)
            .ConfigureAwait(false);
    }

    protected abstract void Register(IKernelBuilder builder, string model);
}