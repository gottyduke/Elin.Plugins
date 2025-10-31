using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    private static readonly HarmonyMethod _testStub = new(typeof(MethodInfoDetail), nameof(StubILPatch));

    internal static readonly HashSet<MethodBase> IncompatibleCalls = [];

    private static bool _nestedStub;

    private static IEnumerable<CodeInstruction> StubILPatch(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions) {
            if (instruction.operand is MethodBase method && !_nestedStub) {
                _nestedStub = true;
                var invalidSubCall = method.TestIncompatibleIl();
                _nestedStub = false;

                if (invalidSubCall) {
                    throw new MissingMethodException();
                }
            }

            yield return instruction;
        }
    }

    extension(MethodBase methodInfo)
    {
        public bool TestIncompatibleIl()
        {
            if (IncompatibleCalls.Contains(methodInfo)) {
                return true;
            }

            var processor = CwlMod.SharedHarmony.CreateProcessor(methodInfo);
            try {
                processor.AddTranspiler(_testStub);
                processor.Patch();
            } catch {
                IncompatibleCalls.Add(methodInfo);
                return true;
                // noexcept
            } finally {
                processor.Unpatch(_testStub.method);
            }

            return false;
        }

        public string GetDetail(bool full = true)
        {
            var decl = methodInfo.DeclaringType;
            if (decl is null) {
                return methodInfo.Name;
            }

            var name = full ? decl.FullName! : decl.Name;

            if (decl.IsGenericType) {
                var genericArgs = string.Join(",", decl.GenericTypeArguments.Select(t => t.FullName ?? t.Name));
                name += $"<{genericArgs}>";
            }

            if (IncompatibleCalls.Contains(methodInfo)) {
                name = "cwl_ui_invalid_patch".lang() + name;
            }

            return $"{name}.{methodInfo.Name}";
        }


        private string GetAssemblyQualifiedDetail(bool full, bool colorize, bool includeParams)
        {
            var asmName = TypeQualifier.GetMappedAssemblyName(methodInfo.Module.Assembly);
            if (colorize) {
                asmName = asmName.TagColor(0x7676a7);
            }

            var methodDetail = methodInfo.GetDetail(full);
            if (includeParams) {
                methodDetail += $" ({methodInfo.GetParameters().Join()})";
            }

            return $"{asmName}::{methodDetail}";
        }

        public string GetAssemblyDetail(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, false, false);
        }

        public string GetAssemblyDetailColor(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, true, false);
        }

        public string GetAssemblyDetailParams(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, false, true);
        }

        public string GetAssemblyDetailParamsColor(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, true, true);
        }
    }
}