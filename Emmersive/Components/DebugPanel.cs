using Cwl.Helper.Unity;

namespace Emmersive.Components;

internal class DebugPanel
{
    internal static DebugPanel? _debugger;

    private ProgressIndicator? _debugProgress;

    internal static void EnableDebugView()
    {
        _debugger ??= new();
        _debugger.Show();
    }

    internal void Show()
    {
        _debugProgress?.Kill();
    }

    internal void Kill()
    {
    }
}