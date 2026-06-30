using UnityEngine;

namespace Emmersive.API.ThirdParty;

public static class EmEvent
{
    /// <summary>
    ///     args = <see cref="int" />,
    ///     current: 4
    /// </summary>
    public const string EmmersiveReady = "emmersive.mod_ready";

    /// <summary>
    ///     args = <see cref="System.Collections.Generic.IDictionary[string, object?]" />
    /// </summary>
    public const string EmmersiveCharaContext = "emmersive.ctx_chara";

    /// <summary>
    ///     example usage of registering handlers for Emmersive events,
    ///     call this during <see cref="Awake()" />
    /// </summary>
    public static void RegisterHandlers()
    {
        // check for Emmersive version
        BaseModManager.SubscribeEvent(EmmersiveReady, args => {
            if (args is < 4) {
                // incompatible
                Debug.Log($"incompatible version, required: >= 4, current: {args}");
            }
        });
    }
}