using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Helper.Extensions;

public static class PatchInfoExt
{
    private static readonly Dictionary<PatchInfo, List<MethodInfo>> _tested = [];

    extension(PatchInfo info)
    {
        public KeyValuePair<string, Patch[]>[] AllPatches => [
            new("PREFIX", info.prefixes),
            new("POSTFIX", info.postfixes),
            new("TRANSPILER", info.transpilers),
        ];

        public void DumpPatchDetails(StringBuilder sb)
        {
            foreach (var (type, patcher) in info.AllPatches) {
                foreach (var patch in patcher) {
                    sb.AppendLine($" +{type,-10}\t{patch.PatchMethod.GetAssemblyDetailColor(false)}".TagColor(0x2f2d2d));
                }
            }
        }

        [SwallowExceptions]
        public List<MethodInfo> TestIncompatiblePatch()
        {
            if (_tested.TryGetValue(info, out var invalids)) {
                return invalids;
            }

            invalids = info.AllPatches
                .SelectMany(kv => kv.Value)
                .Select(p => p.PatchMethod)
                .Where(MethodInfoDetail.TestIncompatibleIl)
                .ToList();

            return _tested[info] = invalids;
        }
    }
}