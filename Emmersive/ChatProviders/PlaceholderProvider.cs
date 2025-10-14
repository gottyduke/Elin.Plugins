using System.Collections.Generic;
using Microsoft.SemanticKernel;
using YKF;

namespace Emmersive.ChatProviders;

internal class PlaceholderProvider : ChatProviderBase
{
    public override string Id { get; set; } = "placeholder";
    public override IReadOnlyList<string> Models { get; } = [];
    public override string CurrentModel { get; set; } = "placeholder";
    public override PromptExecutionSettings ExecutionSettings { get; set; } = new();
    public override bool IsAvailable => false;


    public override void OnLayout(YKLayout layout)
    {
    }

    protected override void Register(IKernelBuilder builder, string model)
    {
    }
}