using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using UnityEngine;

namespace Cwl.Patches.Things;

internal class FixedGunSocketPatch
{
    [CwlThingOnCreateEvent]
    internal static void ApplyGunSocket(Thing thing)
    {
        if (thing is not { source: { } row, IsRangedWeapon: true, IsMeleeWithAmmo: false }) {
            return;
        }

        var sockets = thing.sockets?.Count ?? 0;
        var required = row.tag.FirstOrDefault(t => t.StartsWith("addSocket_"))?.Split('_')[1].AsInt(0) ?? 0;
        var needed = Mathf.Max(required - sockets, 0);
        for (var i = 0; i < needed; ++i) {
            thing.AddSocket();
        }
    }
}