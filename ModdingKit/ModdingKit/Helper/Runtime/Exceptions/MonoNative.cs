using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarmonyLib;
using Debug = UnityEngine.Debug;

namespace EModding.Helper.Runtime.Exceptions;

public static class MonoNative
{
    // https://github.com/Unity-Technologies/mono/tree/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a
    private const string Mono = "mono-2.0-bdwgc";

    private static readonly ConstructorInfo? _methodHandleCtor =
        AccessTools.Constructor(typeof(RuntimeMethodHandle), [typeof(IntPtr)]);

    private static readonly AccessTools.FieldRef<StackFrame, long>? _methodAddress = CreateAddressAccessor();

    private static IntPtr _domain;
    private static bool? _available;

    public static bool Available => _available ??= TryInit();

    public static long GetMethodAddress(StackFrame frame)
    {
        try {
            return _methodAddress?.Invoke(frame) ?? 0L;
        } catch {
            return 0L;
            // noexcept
        }
    }

    public static MethodBase? MethodFromAddress(long address)
    {
        if (!Available || address == 0L) {
            return null;
        }

        try {
            var jitInfo = mono_jit_info_table_find(_domain, (IntPtr)address);
            return jitInfo == IntPtr.Zero ? null : MethodFromHandle(mono_jit_info_get_method(jitInfo));
        } catch {
            return null;
            // noexcept
        }
    }

    public static MethodBase? MethodFromStackFrame(StackFrame frame)
    {
        return MethodFromAddress(GetMethodAddress(frame));
    }

    public static string? MethodNameFromAddress(long address)
    {
        if (!Available || address == 0L) {
            return null;
        }

        try {
            var jitInfo = mono_jit_info_table_find(_domain, (IntPtr)address);
            if (jitInfo == IntPtr.Zero) {
                return null;
            }

            var method = mono_jit_info_get_method(jitInfo);
            if (method == IntPtr.Zero) {
                return null;
            }

            // char* marshal
            var name = Marshal.PtrToStringAnsi(mono_method_get_reflection_name(method));
            return string.IsNullOrEmpty(name) ? null : name;
        } catch {
            return null;
            // noexcept
        }
    }

    public static long CodeStartFromAddress(long address)
    {
        if (!Available || address == 0L) {
            return 0L;
        }

        try {
            var jitInfo = mono_jit_info_table_find(_domain, (IntPtr)address);
            return jitInfo == IntPtr.Zero ? 0L : mono_jit_info_get_code_start(jitInfo).ToInt64();
        } catch {
            return 0L;
            // noexcept
        }
    }

    // jitted methods are compiled
    public static bool IsJitted(long address)
    {
        return CodeStartFromAddress(address) != 0L;
    }

    private static MethodBase? MethodFromHandle(IntPtr methodHandle)
    {
        if (methodHandle == IntPtr.Zero || _methodHandleCtor is null) {
            return null;
        }

        try {
            var handle = (RuntimeMethodHandle)_methodHandleCtor.Invoke([methodHandle]);
            return MethodBase.GetMethodFromHandle(handle, default);
        } catch {
            return null;
            // noexcept
        }
    }

    private static AccessTools.FieldRef<StackFrame, long>? CreateAddressAccessor()
    {
        try {
            // (gpointer)_MonoJitInfo->code_start
            return AccessTools.FieldRefAccess<StackFrame, long>("methodAddress");
        } catch {
            return null;
            // noexcept
        }
    }

    private static bool TryInit()
    {
        try {
            if (IntPtr.Size != sizeof(long) || _methodHandleCtor is null || _methodAddress is null) {
                return false;
            }

            _domain = mono_domain_get();
            if (_domain == IntPtr.Zero) {
                return false;
            }

            var probe = AccessTools.DeclaredMethod(typeof(MonoNative), nameof(TestCompileInfo));
            if (probe is null) {
                return false;
            }

            var code = mono_compile_method(probe.MethodHandle.Value);
            if (code == IntPtr.Zero) {
                return false;
            }

            // code = mono_get_addr_from_ftnptr
            var resolved = mono_jit_info_table_find(_domain, code + 0x1);
            if (resolved == IntPtr.Zero) {
                return false;
            }

            return MethodFromHandle(mono_jit_info_get_method(resolved))?.MethodHandle.Value == probe.MethodHandle.Value;
        } catch (Exception ex) {
            Debug.Log($"#modding-kit mono native frame resolution unavailable, falling back to text parsing: {ex.Message}");
            return false;
            // noexcept
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static long TestCompileInfo()
    {
        return 0xDEADBEEF;
    }

    // https://github.com/Unity-Technologies/mono/blob/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a/mono/metadata/debug-helpers.h#L47
    // char* mono_method_get_reflection_name(MonoMethod*)
    [DllImport(Mono)]
    private static extern IntPtr mono_method_get_reflection_name(IntPtr method);

    // https://github.com/Unity-Technologies/mono/blob/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a/mono/metadata/object.h#L332
    // void* mono_compile_method(MonoMethod*)
    [DllImport(Mono)]
    private static extern IntPtr mono_compile_method(IntPtr method);

    // https://github.com/Unity-Technologies/mono/blob/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a/mono/metadata/appdomain.h#L77
    // MonoDomain* mono_domain_get(void)
    [DllImport(Mono)]
    private static extern IntPtr mono_domain_get();

    // https://github.com/Unity-Technologies/mono/blob/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a/mono/metadata/appdomain.h#L155
    // MonoJitInfo* mono_jit_info_table_find(MonoDomain*, void*)
    [DllImport(Mono)]
    private static extern IntPtr mono_jit_info_table_find(IntPtr domain, IntPtr addr);

    /* MonoJitInfo accessors */

    // https://github.com/Unity-Technologies/mono/blob/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a/mono/metadata/appdomain.h#L160
    // void* mono_jit_info_get_code_start(MonoJitInfo*)
    [DllImport(Mono)]
    private static extern IntPtr mono_jit_info_get_code_start(IntPtr jitInfo);

    // https://github.com/Unity-Technologies/mono/blob/7c55ef821d12f12c0f9933e0bf7abedf245d7c9a/mono/metadata/appdomain.h#L166
    // MonoMethod* mono_jit_info_get_method(MonoJitInfo*)
    [DllImport(Mono)]
    private static extern IntPtr mono_jit_info_get_method(IntPtr jitInfo);
}