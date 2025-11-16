using System;
using System.Collections.Generic;
using ReflexCLI.Attributes;
using Steamworks;

namespace ElinTogether.Helper;

[ConsoleCommandClassCustomizer("emp.native")]
internal sealed class StringAllocator : IDisposable
{
    private readonly Dictionary<string, InteropHelp.UTF8StringHandle> _handles = new(StringComparer.Ordinal);

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private readonly object _lock = new();


    public static StringAllocator Shared => field ??= new();

    public void Dispose()
    {
        lock (_lock) {
            foreach (var handle in _handles.Values) {
                handle.Dispose();
            }

            _handles.Clear();
        }
    }

    public IntPtr Pin(string sz)
    {
        if (string.IsNullOrWhiteSpace(sz)) {
            return IntPtr.Zero;
        }

        lock (_lock) {
            if (!_handles.TryGetValue(sz, out var handle)) {
                _handles[sz] = handle = new(sz);
            }

            var success = false;
            handle.DangerousAddRef(ref success);
            return handle.DangerousGetHandle();
        }
    }

    [ConsoleCommand("unpin_string_pool")]
    public static void UnpinSharedStringHandles()
    {
        Shared.Dispose();
    }
}