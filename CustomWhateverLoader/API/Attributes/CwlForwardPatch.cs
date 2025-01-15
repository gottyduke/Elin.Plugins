using System;
using System.Reflection;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.API;

/// <summary>
///     Because Harmony X bundled with BIE6pre1 doesn't have HarmonyOptional
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class CwlForwardPatch : HarmonyPatch
{
    private static bool _compatible = true;
    private static HarmonyMethod? _lastPatch;

    public CwlForwardPatch()
    {
    }

    public CwlForwardPatch(int enableAfter)
    {
        _compatible = enableAfter <= GameVersion.Int();
    }

    public CwlForwardPatch(int major, int minor, int patch)
        : this(major * 1000000 + minor * 1000 + patch)
    {
    }

    // forward
    public CwlForwardPatch(Type declaringType, string methodName)
        : base(declaringType, methodName)
    {
        EnableIfCompatible();
    }

    // forward
    public CwlForwardPatch(Type declaringType, string methodName, params Type[] argumentTypes)
        : base(declaringType, methodName, argumentTypes)
    {
        EnableIfCompatible();
    }

    // forward
    public CwlForwardPatch(
        Type declaringType,
        string methodName,
        Type[] argumentTypes,
        ArgumentType[] argumentVariations)
        : base(declaringType, methodName, argumentTypes, argumentVariations)
    {
        EnableIfCompatible();
    }

    // forward
    public CwlForwardPatch(Type declaringType, string methodName, MethodType methodType)
        : base(declaringType, methodName, methodType)
    {
        EnableIfCompatible();
    }

    private void EnableIfCompatible()
    {
        if (_compatible) {
            _lastPatch = info;
        } else {
            info = _lastPatch;
        }
    }

    private static MethodInfo TargetMethod()
    {
        var abp = AccessTools.FirstInner(
            AccessTools.TypeByName("AttributePatch"),
            t => t.Name == "<>c");
        return AccessTools.Method(abp, "<Create>b__4_0");
    }

    [HarmonyPostfix]
    private static void OnAttributeQualify(object attr, ref bool __result)
    {
        __result = __result || attr is CwlForwardPatch;
    }
}