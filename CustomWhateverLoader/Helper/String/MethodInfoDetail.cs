using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    public static string GetDetail(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.DeclaringType!;
        return $"{(full ? decl.FullName : decl.Name)}.{methodInfo.Name}";
    }

    public static string GetAssemblyDetail(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.DeclaringType!;
        return $"{decl.Assembly.GetName().Name}::{methodInfo.GetDetail(full)}";
    }

    public static string GetAssemblyDetailColor(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.DeclaringType!;
        return $"{decl.Assembly.GetName().Name.TagColor(0x7676a7)}::{methodInfo.GetDetail(full)}";
    }

    public static string GetAssemblyDetailParams(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.DeclaringType!;
        return $"{decl.Assembly.GetName().Name}::{methodInfo.GetDetail(full)} ({methodInfo.GetParameters().Join()})";
    }

    public static string GetAssemblyDetailParamsColor(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.DeclaringType!;
        return
            $"{decl.Assembly.GetName().Name.TagColor(0x7676a7)}::{methodInfo.GetDetail(full)} ({methodInfo.GetParameters().Join()})";
    }

    public static void AppendPatchInfo(this StringBuilder sb, PatchInfo patchInfo)
    {
        foreach (var patch in patchInfo.prefixes) {
            sb.AppendLine($"\t+PREFIX: {patch.PatchMethod.GetAssemblyDetailColor(false)}".TagColor(0x2f2d2d));
        }

        foreach (var patch in patchInfo.postfixes) {
            sb.AppendLine($"\t+POSTFIX: {patch.PatchMethod.GetAssemblyDetailColor(false)}".TagColor(0x2f2d2d));
        }

        foreach (var patch in patchInfo.transpilers) {
            sb.AppendLine($"\t+TRANSPILER: {patch.PatchMethod.GetAssemblyDetailColor(false)}".TagColor(0x2f2d2d));
        }
    }
}