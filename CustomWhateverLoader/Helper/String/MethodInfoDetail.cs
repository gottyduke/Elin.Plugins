using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    internal static readonly HashSet<MethodInfo> InvalidCalls = [];

    public static void AppendPatchInfo(this StringBuilder sb, PatchInfo patchInfo)
    {
        foreach (var (type, patcher) in patchInfo.AllPatches) {
            foreach (var patch in patcher) {
                var patchType = type;
                if (InvalidCalls.Contains(patch.PatchMethod)) {
                    patchType += "cwl_ui_invalid_patch".Loc();
                }

                sb.AppendLine($"\t+{patchType}: {patch.PatchMethod.GetAssemblyDetailColor(false)}".TagColor(0x2f2d2d));
            }
        }
    }

    extension(MethodInfo methodInfo)
    {
        public string GetDetail(bool full = true)
        {
            var decl = methodInfo.DeclaringType!;
            return $"{(full ? decl.FullName : decl.Name)}.{methodInfo.Name}";
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
            return
                $"{decl.Assembly.GetName().Name.TagColor(0x7676a7)}::{methodInfo.GetDetail(full)} ({methodInfo.GetParameters().Join()})";
        }
    }
}