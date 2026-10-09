using System;

namespace Emmersive.API.ThirdParty;

public static class EmEvent
{
    public const int ApiLevel = 5;

    /// <summary>
    ///     args = <see cref="int" />,
    ///     current: 5
    /// </summary>
    public const string EmmersiveReady = "emmersive.mod_ready";
    public const string SceneLine = "emmersive.scene_line";
    public const string SceneRequestEnd = "emmersive.scene_request_end";

    public static event Action<SceneLineArgs>? OnSceneLine;
    public static event Action<SceneRequestArgs>? OnSceneRequestEnd;

    internal static void RaiseSceneLine(SceneLineArgs args)
    {
        Invoke(OnSceneLine, args);
    }

    internal static void RaiseSceneRequestEnd(SceneRequestArgs args)
    {
        Invoke(OnSceneRequestEnd, args);
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
