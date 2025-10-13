using System;
using System.Threading;
using Cwl.Helper.Exceptions;
using Cysharp.Threading.Tasks;

namespace Cwl.Helper.Unity;

public static class UniTasklet
{
    public static CancellationTokenSource Timeout(float timeout)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
        return cts;
    }

    extension(UniTask tasklet)
    {
        public void RunOnPool(CancellationToken token = default)
        {
            UniTask.RunOnThreadPool(() => tasklet, false, token).Forget(ExceptionProfile.DefaultExceptionHandler);
        }
    }
}