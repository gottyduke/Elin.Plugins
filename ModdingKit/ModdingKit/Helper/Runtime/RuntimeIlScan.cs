using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using EModding.Helper.Runtime.Exceptions;
using Mono.Cecil;

namespace EModding.Helper.Runtime;

internal static class RuntimeIlScan
{
    private static readonly Dictionary<short, OpCode> _opCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null))
        .ToDictionary(op => op.Value);
    private static readonly ReaderParameters _asmReaderParam = new() {
        AssemblyResolver = TypeLoader.CecilResolver,
    };

    internal static readonly Dictionary<MethodBase, bool> CheckedCalls = [];

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

    extension(MethodBase methodInfo)
    {
        public bool TestIncompatibleIl()
        {
            if (CheckedCalls.TryGetValue(methodInfo, out var incompatible)) {
                return incompatible;
            }

            if (methodInfo.DeclaringType is null) {
                return CheckedCalls[methodInfo] = false;
            }

            if (TryTest(methodInfo, out incompatible) && incompatible) {
                return CheckedCalls[methodInfo] = true;
            }

            if (!HasModuleOnDisk(methodInfo)) {
                return CheckedCalls[methodInfo] = false;
            }

            try {
                var asm = AssemblyDefinition.ReadAssembly(methodInfo.Module.FullyQualifiedName, _asmReaderParam);
                var def = asm?.MainModule.LookupToken(methodInfo.MetadataToken) as MethodDefinition;
                incompatible = TestIncompatibleDef(def);
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                incompatible = false;
                // noexcept
            }

            return CheckedCalls[methodInfo] = incompatible;

            bool TestIncompatibleDef(MethodDefinition? methodDef, bool nested = false)
            {
                if (methodDef?.Body?.Instructions is not { Count: > 0 } instructions) {
                    return false;
                }

                try {
                    foreach (var il in instructions) {
                        var incompatibleBody = il.Operand switch {
                            MethodReference mr => mr.DeclaringType is not ArrayType
                                                  && (mr.Resolve() is not { } targetDef
                                                      || (!nested && TestIncompatibleDef(targetDef, true))),
                            FieldReference fr => fr.Resolve() is null,
                            TypeReference { ContainsGenericParameter: false } tr => tr.Resolve() is null,
                            _ => false,
                        };
                        if (incompatibleBody) {
                            return true;
                        }
                    }
                } catch (Exception ex) {
                    DebugThrow.Void(ex);
                    // noexcept
                }
                return false;
            }
        }
    }

    private static bool HasModuleOnDisk(MethodBase method)
    {
        var assembly = method.Module.Assembly;
        return !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location);
    }
}