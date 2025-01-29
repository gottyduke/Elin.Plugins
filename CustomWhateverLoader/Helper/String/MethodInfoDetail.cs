using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    public static string GetDetail(this MethodInfo methodInfo)
    {
        var decl = methodInfo.DeclaringType!;
        return $"{decl.FullName}.{methodInfo.Name}";
    }

    public static void AppendPatchInfo(this StringBuilder sb, PatchInfo patchInfo)
    {
        foreach (var patch in patchInfo.prefixes) {
            sb.AppendLine($"\t|Prefix: {patch.PatchMethod.GetDetail()}");
        }

        foreach (var patch in patchInfo.postfixes) {
            sb.AppendLine($"\t|Postfix: {patch.PatchMethod.GetDetail()}");
        }

        foreach (var patch in patchInfo.transpilers) {
            sb.AppendLine($"\t|Transpiler: {patch.PatchMethod.GetDetail()}");
        }
    }
}