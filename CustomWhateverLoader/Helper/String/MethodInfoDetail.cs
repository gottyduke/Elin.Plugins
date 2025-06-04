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
        return $"<color=#7676a7>{decl.Assembly.GetName().Name}</color>::{methodInfo.GetDetail(full)}";
    }

    public static void AppendPatchInfo(this StringBuilder sb, PatchInfo patchInfo)
    {
        foreach (var patch in patchInfo.prefixes) {
            sb.Append($"\t<color=#2f2d2d>+PREFIX: {patch.PatchMethod.GetAssemblyDetailColor(false)}</color>");
        }

        foreach (var patch in patchInfo.postfixes) {
            sb.Append($"\t<color=#2f2d2d>+POSTFIX: {patch.PatchMethod.GetAssemblyDetailColor(false)}</color>");
        }

        foreach (var patch in patchInfo.transpilers) {
            sb.Append($"\t<color=#2f2d2d>+TRANSPILER: {patch.PatchMethod.GetAssemblyDetailColor(false)}</color>");
        }
    }
}