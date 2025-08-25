using System.Collections.Generic;
using UnityEngine;

namespace Cwl.Helper.Unity;

public static class EffectHelper
{
    public static Effect? GetEffectTemplate(string id)
    {
        var manager = Effect.manager;
        if (manager.effects.map is null) {
            var rod = Effect.Get("rod");
            Object.Destroy(rod);
        }

        return manager.effects.map.GetValueOrDefault(id);
    }
}