using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Emmersive.API;

public interface IChatProvider
{
    public string Id { get; }
    public bool IsAvailable { get; }
    public string CurrentModel { get; }
    public IDictionary<string, object> RequestParams { get; }

    public void Register(IKernelBuilder builder);

    public void MarkUnavailable(string? reason = null);

    public void UpdateAvailability();

    public UniTask<ChatMessageContent> HandleRequest(Kernel kernel, ChatHistory context, CancellationToken token);
}