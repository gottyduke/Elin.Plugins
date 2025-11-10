using System;
using Steamworks;

namespace ElinTogether.Helper;

internal class SteamCallback
{
    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    // shared global event lock - not per instance
    public static readonly object EventLock = new();
}

internal class SteamCallback<T> where T : struct
{
    private static readonly Callback<T> _callback;

    static SteamCallback()
    {
        _callback = Callback<T>.Create(SafeCallback);

        return;

        void SafeCallback(T callbackStruct)
        {
            try {
                Event?.Invoke(callbackStruct);
            } catch (Exception ex) {
                EmpLog.Verbose(ex, "Exception at steam callback {CallbackName}",
                    typeof(T).FullName);
            }
        }
    }

    private static event Action<T>? Event;

    internal static void Add(Action<T> handler)
    {
        lock (SteamCallback.EventLock) {
            Event -= handler;
            Event += handler;
        }
    }

    internal static void Remove(Action<T> handler)
    {
        lock (SteamCallback.EventLock) {
            Event -= handler;
        }
    }

    internal static void Clear()
    {
        lock (SteamCallback.EventLock) {
            Event = null;
        }
    }
}