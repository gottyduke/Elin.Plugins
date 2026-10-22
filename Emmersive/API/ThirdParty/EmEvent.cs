using System;
using System.Collections.Generic;

namespace Emmersive.API.ThirdParty;

public static class EmEvent
{
    public const int ApiLevel = 6;

    /// <summary>
    ///     args = <see cref="int" />,
    ///     current: 6
    /// </summary>
    public const string EmmersiveReady = "emmersive.mod_ready";
    public const string SceneLine = "emmersive.scene_line";
    public const string SceneRequestEnd = "emmersive.scene_request_end";
    public const string MemoryFactChanged = "emmersive.memory_fact_changed";

    public static event Action<SceneLineArgs>? OnSceneLine;
    public static event Action<SceneRequestArgs>? OnSceneRequestEnd;
    public static event Action<MemoryFactChangedArgs>? OnMemoryFactChanged;

    internal static void RaiseSceneLine(SceneLineArgs args)
    {
        Invoke(OnSceneLine, args);
    }

    internal static void RaiseSceneRequestEnd(SceneRequestArgs args)
    {
        Invoke(OnSceneRequestEnd, args);
    }

    internal static void RaiseMemoryFactChanged(MemoryFactChangedArgs args)
    {
        Invoke(OnMemoryFactChanged, args);

        var data = new Dictionary<string, object> {
            ["uid"] = args.Uid,
            ["name"] = args.Name,
            ["change"] = args.Change switch {
                MemoryFactChange.Added => "added",
                MemoryFactChange.Updated => "updated",
                _ => "forgotten",
            },
            ["fact"] = args.Fact,
            ["importance"] = args.Importance,
        };

        if (args.Change == MemoryFactChange.Updated) {
            data["old_fact"] = args.OldFact ?? "";
            data["old_importance"] = args.OldImportance;
        }

        BaseModManager.PublishEvent<IDictionary<string, object>>(MemoryFactChanged, data);
    }

    private static void Invoke<T>(Action<T>? handlers, T args)
    {
        if (handlers is null) {
            return;
        }

        foreach (var handler in handlers.GetInvocationList()) {
            try {
                ((Action<T>)handler)(args);
            } catch (Exception ex) {
                EmMod.Warn($"[EmEvent] {handler.Method.DeclaringType?.FullName}.{handler.Method.Name} threw: {ex}");
            }
        }
    }
}