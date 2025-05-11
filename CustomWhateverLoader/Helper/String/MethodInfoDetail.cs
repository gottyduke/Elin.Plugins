using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    public static string GetDetail(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.ReflectedType!;
        return $"{(full ? decl.FullName : decl.Name)}.{methodInfo.Name}";
    }

    public static string GetAssemblyDetail(this MethodInfo methodInfo, bool full = true)
    {
        var decl = methodInfo.ReflectedType!;
        return $"{decl.Assembly.GetName().Name}::{methodInfo.GetDetail(full)}";
    }

    public static void AppendPatchInfo(this StringBuilder sb, PatchInfo patchInfo)
    {
        foreach (var patch in patchInfo.prefixes) {
            sb.AppendLine($"\t+Prefix: {patch.PatchMethod.GetAssemblyDetail(false)}");
        }

        foreach (var patch in patchInfo.postfixes) {
            sb.AppendLine($"\t+Postfix: {patch.PatchMethod.GetAssemblyDetail(false)}");
        }

        foreach (var patch in patchInfo.transpilers) {
            sb.AppendLine($"\t+Transpiler: {patch.PatchMethod.GetAssemblyDetail(false)}");
        }
    }
}