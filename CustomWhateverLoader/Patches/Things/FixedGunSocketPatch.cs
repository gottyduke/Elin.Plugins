using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;

namespace Cwl.Patches.Things;

internal class FixedGunSocketPatch : EClass
{
    [CwlThingOnCreateEvent]
    internal static void ApplyGunSocket(Thing thing)
    {
        if (thing is not { source: { } row, IsRangedWeapon: true, IsMeleeWithAmmo: false }) {
            return;
        }

        var sockets = thing.sockets ??= [];
        var tags = row.tag
            .Where(t => t.StartsWith("addSocket"))
            .ToArray();
        if (tags.Length == 0) {
            return;
        }

        if (row.tag.Contains("noRandomSocket")) {
            sockets.Clear();
        }

        var emptyRequired = 0;
        foreach (var socketExpr in tags) {
            thing.AddSocket();

            var socket = socketExpr.ExtractInBetween('(', ')');
            if (socket.IsEmptyOrNull) {
                emptyRequired++;
            } else {
                thing.ApplyRangedSocket(socket);
            }
        }

        sockets.RemoveAll(s => s == 0);
        for (var i = 0; i < emptyRequired; ++i) {
            sockets.Add(0);
        }
    }
}