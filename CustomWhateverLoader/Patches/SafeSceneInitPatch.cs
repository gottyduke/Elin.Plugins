using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches;

[HarmonyPatch(typeof(Scene), nameof(Scene.Init))]
public class SafeSceneInitPatch
{
    internal static bool SafeToCreate;
    private static ILookup<Scene.Mode, SceneCallback>? _preLut;
    private static ILookup<Scene.Mode, SceneCallback>? _postLut;
    private static readonly List<(Scene.Mode, SceneCallback)> _preCallbacks = [];
    private static readonly List<(Scene.Mode, SceneCallback)> _postCallbacks = [];

    [HarmonyPrefix]
    internal static void OnPreSceneInit(Scene.Mode newMode)
    {
        _preLut ??= _preCallbacks.ToLookup(x => x.Item1, x => x.Item2);
        foreach (var (callback, defer) in _preLut[newMode])
            try {
                if (defer) {
                    CoroutineHelper.Deferred(() => callback.Invoke(null));
                } else {
                    callback.Invoke(null);
                }
            } catch (Exception ex) {
                CwlMod.Warn<SafeSceneInitPatch>("cwl_warn_processor".Loc("pre_scene_init", newMode, ex));
                // noexcept
            }
    }

    [HarmonyPostfix]
    internal static void OnPostSceneInit(Scene.Mode newMode)
    {
        SafeToCreate = newMode switch {
            Scene.Mode.Title => false,
            Scene.Mode.StartGame => true,
            _ => SafeToCreate,
        };

        _postLut ??= _postCallbacks.ToLookup(x => x.Item1, x => x.Item2);
        foreach (var (callback, defer) in _postLut[newMode])
            try {
                if (defer) {
                    CoroutineHelper.Deferred(() => callback.Invoke(null));
                } else {
                    callback.Invoke(null);
                }
            } catch (Exception ex) {
                CwlMod.Warn<SafeSceneInitPatch>("cwl_warn_processor".Loc("post_scene_init", newMode, ex));
                // noexcept
            }
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlSceneInitEvent onSceneInit)
    {
        var callback = (onSceneInit.Mode, new SceneCallback(
            MethodInvoker.GetHandler(method, true),
            onSceneInit.Defer));

        if (onSceneInit.PreInit) {
            _preCallbacks.Add(callback);
        } else {
            _postCallbacks.Add(callback);
        }

        var initType = onSceneInit.PreInit ? "pre" : "post";
        CwlMod.Log<SafeSceneInitPatch>("cwl_log_processor_add".Loc($"{initType}_scene_init",
            onSceneInit.Mode,
            method.GetAssemblyDetail(false)));
    }

    internal static void RebuildLookupTables()
    {
        _preLut = null;
        _postLut = null;
    }

    private record SceneCallback(FastInvokeHandler Callback, bool ShouldDefer);
}