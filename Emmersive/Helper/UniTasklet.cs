using System.Threading;
using Cysharp.Threading.Tasks;
using EModding.Helper.Runtime.Exceptions;

namespace Emmersive.Helper;

public static class UniTasklet
{
    public static CancellationToken GameToken = EmMod.Instance.GetCancellationTokenOnDestroy();

    public static CancellationTokenSource SceneCts
    {
        get => field ??= new();
        private set;
    }

    [ElinPostSceneInit]
    private static void OnSceneExit(Scene.Mode mode)
    {
        if (mode != Scene.Mode.Title) {
            return;
        }

        SceneCts.Cancel();
        SceneCts.Dispose();
        SceneCts = null!;
    }

    extension(UniTask task)
    {
        public void ForgetEx()
        {
            task.AttachExternalCancellation(GameToken)
                .SuppressCancellationThrow()
                .Forget(ExceptionProfile.DefaultExceptionHandler);
        }
    }
}