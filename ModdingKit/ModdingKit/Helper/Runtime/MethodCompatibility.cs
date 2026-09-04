using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using EModding.Helper.Runtime.Exceptions;
using Mono.Cecil;

namespace EModding.Helper.Runtime;

public static class MethodCompatibility
{
    private static readonly ReaderParameters _asmReaderParam = new() {
        AssemblyResolver = TypeLoader.CecilResolver,
    };

    internal static readonly Dictionary<MethodBase, bool> CheckedCalls = [];

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

            // 先问运行时：它的 token 解析和 jit 走同一条路，答案就是这方法真跑起来时的答案，
            // 而且不挑模块在不在磁盘上，roslyn 脚本一样测得了
            if (RuntimeIlScan.TryTest(methodInfo, out incompatible) && incompatible) {
                return CheckedCalls[methodInfo] = true;
            }

            // 运行时没挑出毛病不代表没有：它和 cecil 用的是两套解析器，覆盖面互补，
            // 所以只要模块在磁盘上就再核一遍，不让原本查得出来的情况漏掉
            if (!HasModuleOnDisk(methodInfo)) {
                return CheckedCalls[methodInfo] = false;
            }

            try {
                var asm = AssemblyDefinition.ReadAssembly(methodInfo.Module.FullyQualifiedName, _asmReaderParam);
                // 按 metadata token 精确定位。原先按名字加参数类型名去找，
                // 泛型和重载上会错配，找不到时又静默当作兼容
                var def = asm?.MainModule.LookupToken(methodInfo.MetadataToken) as MethodDefinition;
                incompatible = TestIncompatibleDef(def);
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                // 读不出来只说明判定不了，不等于不兼容。原先这里返回 true，
                // 任何读取失败都会把一个好方法标成红的
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