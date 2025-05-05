using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using KonoExt.Common;

namespace KonoExt.Patches;

internal class PostLoadEvent : EClass
{
    [CwlPostLoad]
    internal static void AddItemIfMissing(GameIOProcessor.GameIOContext context)
    {
        if (!core.IsGameStarted) {
            return;
        }

        pc.GainAbility(Constants.SpKonoExplosionId);
    }
}