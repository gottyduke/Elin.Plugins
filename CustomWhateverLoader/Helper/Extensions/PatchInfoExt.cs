using System.Collections.Generic;
using System.Reflection;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Helper.Extensions;

public static class PatchInfoExt
{
    private static readonly Dictionary<PatchInfo, List<MethodInfo>> _tested = [];
    private static readonly HarmonyMethod _testStub = new(typeof(PatchInfoExt), nameof(StubILPatch));

    private static IEnumerable<CodeInstruction> StubILPatch(IEnumerable<CodeInstruction> instructions)
    {
        return instructions;
    }

    extension(PatchInfo info)
    {
        public KeyValuePair<string, Patch[]>[] AllPatches => [
            new("PREFIX", info.prefixes),
            new("POSTFIX", info.postfixes),
            new("TRANSPILER", info.transpilers),
        ];

        [SwallowExceptions]
        public List<MethodInfo> TestIncompatiblePatch()
        {
            if (_tested.TryGetValue(info, out var invalids)) {
                return invalids;
            }

            invalids = [];

            foreach (var (_, patcher) in info.AllPatches) {
                foreach (var patch in patcher) {
                    var method = patch.PatchMethod;
                    try {
                        CwlMod.SharedHarmony.Patch(method, transpiler: _testStub);
                        CwlMod.SharedHarmony.Unpatch(method, _testStub.method);
                    } catch {
                        invalids.Add(method);
                        // noexcept
                    }
                }
            }

            MethodInfoDetail.InvalidCalls.UnionWith(invalids);
            return _tested[info] = invalids;
        }
    }
}