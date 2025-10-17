using System;
using System.Threading;
using Cwl.Helper.Exceptions;
using Cysharp.Threading.Tasks;

namespace Cwl.Helper.Unity;

public static class UniTasklet
{
    public static CancellationToken GameToken = CwlMod.Instance.GetCancellationTokenOnDestroy();

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