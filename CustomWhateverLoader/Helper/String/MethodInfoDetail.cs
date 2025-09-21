using System.Collections.Generic;
using System.Reflection;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    private static readonly HarmonyMethod _testStub = new(typeof(MethodInfoDetail), nameof(StubILPatch));

    internal static readonly HashSet<MethodInfo> InvalidCalls = [];

    private static IEnumerable<CodeInstruction> StubILPatch(IEnumerable<CodeInstruction> instructions)
    {
        return instructions;
    }

    extension(MethodInfo methodInfo)
    {
        public bool TestIncompatibleIl()
        {
            if (InvalidCalls.Contains(methodInfo)) {
                return true;
            }

            try {
                var processor = CwlMod.SharedHarmony.CreateProcessor(methodInfo);
                processor.AddTranspiler(_testStub);
                processor.Patch();
                processor.Unpatch(_testStub.method);
            } catch {
                InvalidCalls.Add(methodInfo);
                return true;
                // noexcept
            }

            return false;
        }


        public string GetDetail(bool full = true)
        {
            var decl = methodInfo.DeclaringType!;
            var detail = $"{(full ? decl.FullName : decl.Name)}.{methodInfo.Name}";
            return InvalidCalls.Contains(methodInfo)
                ? "cwl_ui_invalid_patch".Loc() + detail
                : detail;
        }

        public string GetAssemblyDetail(bool full = true)
        {
            var decl = methodInfo.DeclaringType!;
            return $"{decl.Assembly.GetName().Name}::{methodInfo.GetDetail(full)}";
        }

        public string GetAssemblyDetailColor(bool full = true)
        {
            var decl = methodInfo.DeclaringType!;
            return $"{decl.Assembly.GetName().Name.TagColor(0x7676a7)}::{methodInfo.GetDetail(full)}";
        }

        public string GetAssemblyDetailParams(bool full = true)
        {
            var decl = methodInfo.DeclaringType!;
            return $"{decl.Assembly.GetName().Name}::{methodInfo.GetDetail(full)} ({methodInfo.GetParameters().Join()})";
        }

        public string GetAssemblyDetailParamsColor(bool full = true)
        {
            var decl = methodInfo.DeclaringType!;
            return $"{decl.Assembly.GetName().Name.TagColor(0x7676a7)}::{methodInfo.GetDetail(full)} ({methodInfo.GetParameters().Join()})";
        }
    }
}