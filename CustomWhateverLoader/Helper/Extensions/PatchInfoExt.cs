using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Helper.Extensions;

public static class PatchInfoExt
{
    private static readonly Dictionary<PatchInfo, List<MethodInfo>> _tested = [];
    private static readonly HarmonyMethod _testStub = new(typeof(PatchInfoExt), nameof(StubILPatch));

    internal static readonly HashSet<MethodInfo> InvalidCalls = [];
    private static IEnumerable<CodeInstruction> StubILPatch(IEnumerable<CodeInstruction> instructions)
    {
        return instructions;
    }

    extension(PatchInfo info)
    {
        public KeyValuePair<string, Patch[]>[] AllPatches => [
            new("PRE", info.prefixes),
            new("POST", info.postfixes),
            new("IL", info.transpilers),
        ];

        public void DumpPatchDetails(StringBuilder sb)
        {
            foreach (var (type, patcher) in info.AllPatches) {
                foreach (var patch in patcher) {
                    var patchType = type;
                    if (InvalidCalls.Contains(patch.PatchMethod)) {
                        patchType += "cwl_ui_invalid_patch".Loc();
                    }

                    sb.AppendLine($"\t+{patchType}: {patch.PatchMethod.GetAssemblyDetailColor(false)}".TagColor(0x2f2d2d));
                }
            }
        }

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

                    if (InvalidCalls.Contains(method)) {
                        invalids.Add(method);
                        continue;
                    }

                    try {
                        var processor = CwlMod.SharedHarmony.CreateProcessor(method);
                        processor.AddTranspiler(_testStub);
                        processor.Patch();
                        processor.Unpatch(_testStub.method);
                    } catch (MissingMethodException) {
                        invalids.Add(method);
                        // noexcept
                    } catch {
                        // noexcept
                    }
                }
            }

            InvalidCalls.UnionWith(invalids);
            return _tested[info] = invalids;
        }
    }
}