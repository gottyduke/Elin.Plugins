using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using Cwl.Helper.Exceptions;
using Mono.Cecil;

namespace Cwl.Helper;

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

            CheckedCalls[methodInfo] = incompatible;

            if (methodInfo.DeclaringType is null) {
                return false;
            }

            try {
                var asm = AssemblyDefinition.ReadAssembly(methodInfo.Module.FullyQualifiedName, _asmReaderParam);
                var type = asm?.MainModule.GetType(methodInfo.DeclaringType.FullName!.Replace('+', '/'));
                var def = type?.Methods.FirstOrDefault(FindMethod);
                incompatible = TestIncompatibleDef(def);
            } catch (Exception ex) {
                DebugThrow.Void(ex);
                incompatible = true;
                // noexcept
            }

            return CheckedCalls[methodInfo] = incompatible;

            bool FindMethod(MethodDefinition methodDef)
            {
                return methodDef.Name == methodInfo.Name &&
                       methodDef.Parameters
                           .Select(p => p.ParameterType.Name.Replace('+', '/'))
                           .SequenceEqual(methodInfo.GetParameters().Select(p => p.ParameterType.Name.Replace('+', '/')));
            }

            bool TestIncompatibleDef(MethodDefinition? methodDef, bool nested = false)
            {
                if (methodDef?.Body?.Instructions is not { Count: > 0 } instructions) {
                    return false;
                }

                try {
                    foreach (var il in instructions) {
                        var incompatibleBody = il.Operand switch {
                            MethodReference mr => mr.Resolve() is not { } targetDef ||
                                                  (!nested && TestIncompatibleDef(targetDef, true)),
                            FieldReference fr => fr.Resolve() is null,
                            TypeReference tr and not GenericParameter => tr.Resolve() is null,
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
}