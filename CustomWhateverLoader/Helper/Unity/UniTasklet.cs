using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cwl.Helper.Unity;

public static class UniTasklet
{
    public static CancellationToken Timeout(float timeout)
    {
        if (timeout <= 0f) {
            return CancellationToken.None;
        }

        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
        return cts.Token;
    }

    extension(UniTaskVoid tasklet)
    {
        public void RunOnPool()
        {
            UniTask.RunOnThreadPool(tasklet.Forget);
        }
    }
}