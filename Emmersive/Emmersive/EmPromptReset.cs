using System.IO;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Unity;
using Emmersive.Components;
using Emmersive.Contexts;
using Emmersive.Helper;

namespace Emmersive;

internal class EmPromptReset
{
    private static bool _consumeNext;

    internal static void EnablePromptWatcher()
    {
        Directory.CreateDirectory(ResourceFetch.CustomFolder);

        FileWatcherHelper.Register(
            "em_custom_prompts",
            ResourceFetch.CustomFolder,
            "*.txt",
            _ => {
                ResourceFetch.ClearActiveResources();
                RelationContext.Clear();

                var panel = LayerEmmersivePanel.Instance;
                if (panel != null) {
                    // defer to unity main thread
                    CoroutineHelper.Deferred(() => panel.Reopen());
                }
            });
    }

    internal static void SetNotify(bool notify)
    {
        _consumeNext = !notify;
    }

    internal static ScopeExit ScopedNotifyChanges(bool notify)
    {
        SetNotify(notify);

        return new() {
            OnExit = () => SetNotify(notify),
        };
    }
}