using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Cwl.Helper.Unity;

public static class UniTasklet
{
    public static CancellationToken Timeout(float timeout)
    {
        return timeout <= 0f
            ? CancellationToken.None
            : new CancellationTokenSource(TimeSpan.FromSeconds(timeout)).Token;
    }

    extension(UniTaskVoid tasklet)
    {
        public void RunOnPool()
        {
            UniTask.RunOnThreadPool(tasklet.Forget);
        }
    }
}