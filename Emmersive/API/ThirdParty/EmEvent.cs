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
    ///     example usage of registering handlers for Emmersive events,
    ///     call this during <see cref="Awake()" />
    /// </summary>
    public static void RegisterHandlers()
    {
        // check for Emmersive version
        BaseModManager.SubscribeEvent<int>(EmmersiveReady, version => {
            if (version < 4) {
                // incompatible
                Debug.Log($"incompatible version, required: >= 4, current: {version}");
            }
        });
    }
}