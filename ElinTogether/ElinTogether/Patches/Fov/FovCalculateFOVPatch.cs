using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch(typeof(Card), nameof(Card.CalculateFOV))]
internal class FovCalculateFOVPatch
{
    public static bool needOverride = false;

    // check whether this card is a remote player chara (which need to share vision)
    // current PC is not included
    static bool IsRemotePlayerChara(Card card)
    {
        /*
        var session = NetSession.Instance;
        if (session.Connection is not { } connection)
        {
            return false;
        }
        */

        // TODO: dk will fix it
        // currently share all party members' vision
        if (card is Chara &&
            card.Chara.IsPCFaction &&
            !card.Chara.IsPC &&
            card.Chara.IsPCParty) 
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [HarmonyPrefix]
    internal static bool OnCardCalculateFOV(Card __instance)
    {
        if (IsRemotePlayerChara(__instance))
        {
            EmpLog.Verbose($"OnCardCalculateFOV needOverride {__instance.Name}");
            __instance.fov.isPC = true;
            needOverride = true;
        }
        else
        {
            EmpLog.Verbose($"OnCardCalculateFOV dont needOverride {__instance.Name}");
            needOverride = false;
        }
        return true;
    }

    [HarmonyPostfix]
    internal static void OnCardCalculateFOVPost(Card __instance)
    {
        if (needOverride)
        {
            __instance.fov.isPC = false;
        }

        needOverride = false;
    }
}
