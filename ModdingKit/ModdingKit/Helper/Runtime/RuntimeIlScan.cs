using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EModding.Helper.Runtime;

internal static class RuntimeIlScan
{
    private static readonly Dictionary<short, OpCode> _opCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null))
        .ToDictionary(op => op.Value);

    public static bool TryTest(MethodBase method, out bool incompatible, bool nested = false)
    {
        incompatible = false;

        if (GetIl(method) is not { } il || ScanTokens(il) is not { } tokens) {
            return false;
        }

        var module = method.Module;
        var typeArgs = method.DeclaringType is { IsGenericType: true } declaring
            ? declaring.GetGenericArguments()
            : null;
        var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;

        foreach (var token in tokens) {
            MemberInfo? member;

            try {
                member = module.ResolveMember(token, typeArgs, methodArgs);
            } catch (Exception ex) when (ex is MissingMemberException or TypeLoadException) {
                incompatible = true;
                return true;
                // noexcept
            } catch {
                continue;
                // noexcept
            }

            if (nested || member is not MethodBase callee ||
                callee == method || callee.Module != module) {
                continue;
            }

            if (TryTest(callee, out var calleeIncompatible, true) && calleeIncompatible) {
                incompatible = true;
                return true;
            }
        }

        return true;
    }

    private static byte[]? GetIl(MethodBase method)
    {
        try {
            return method.GetMethodBody()?.GetILAsByteArray();
        } catch {
            return null;
            // noexcept
        }
    }

    private static List<int>? ScanTokens(byte[] il)
    {
        var tokens = new List<int>();

        for (var pos = 0; pos < il.Length;) {
            var code = (short)il[pos++];
            if (code == 0xFE) {
                if (pos >= il.Length) {
                    return null;
                }

                code = (short)(0xFE00 | il[pos++]);
            }

            if (!_opCodes.TryGetValue(code, out var op)) {
                return null;
            }

            var size = OperandSize(op.OperandType, il, pos);
            if (size < 0 || pos + size > il.Length) {
                return null;
            }

            if (op.OperandType is OperandType.InlineMethod or
                OperandType.InlineField or
                OperandType.InlineType or
                OperandType.InlineTok) {
                tokens.Add(BitConverter.ToInt32(il, pos));
            }

            pos += size;
        }

        return tokens;
    }

    private static int OperandSize(OperandType type, byte[] il, int pos)
    {
        return type switch {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or
                OperandType.InlineField or
                OperandType.InlineI or
                OperandType.InlineMethod or
                OperandType.InlineSig or
                OperandType.InlineString or
                OperandType.InlineTok or
                OperandType.InlineType or
                OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => pos + 4 <= il.Length ? 4 + BitConverter.ToInt32(il, pos) * 4 : -1,
            _ => -1,
        };
    }
}