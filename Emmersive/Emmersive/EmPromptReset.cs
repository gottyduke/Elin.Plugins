using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.Contexts;

namespace Emmersive;

internal static class EmPromptReset
{
    private static readonly Debouncer _debounce = new(0.3f);

    internal static void EnablePromptWatcher()
    {
        Directory.CreateDirectory(ResourceFetch.CustomFolder);

        FileWatcherHelper.Register(
            "em_custom_prompts",
            ResourceFetch.CustomFolder,
            "*.txt",
            args => {
                var name = args.Name ?? "";
                if (name.EndsWith("dry_run.txt", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Params", StringComparison.OrdinalIgnoreCase)) {
                    return;
                }

                var path = args.FullPath;
                _debounce.Trigger(() => {
                    if (!ResourceFetch.IsUnchangedSinceModWrite(path)) {
                        ReloadPrompts();
                    }
                });
            });
    }

    private static void ReloadPrompts()
    {
        ResourceFetch.ClearActiveResources();
        RelationContext.Clear();

        var panel = LayerEmmersivePanel.Instance;
        if (panel != null) {
            panel.Reopen();
        }
    }
}

internal sealed class Debouncer(float seconds)
{
    private long _last;

    public void Trigger(Action action)
    {
        var stamp = Interlocked.Increment(ref _last);

        UniTask.Post(() => CoroutineHelper.Deferred(() => {
            if (Interlocked.Read(ref _last) != stamp) {
                return;
            }

            action();
        }, seconds));
    }
}