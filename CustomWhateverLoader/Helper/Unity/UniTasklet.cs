using System.Threading;
using Cwl.API.Attributes;
using Cwl.Helper.Exceptions;
using Cysharp.Threading.Tasks;

namespace Cwl.Helper.Unity;

public static class UniTasklet
{
    public static CancellationToken GameToken = CwlMod.Instance.GetCancellationTokenOnDestroy();


    public static CancellationTokenSource SceneCts
    {
        get => field ??= new();
        private set;
    }

    [CwlSceneInitEvent(Scene.Mode.Title)]
    private static void OnSceneExit()
    {
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