using DiscordRPC;
using Erpc.Resources;
using HarmonyLib;

namespace Erpc.States;

[HarmonyPatch]
internal class PlayerStates
{
    private static int _ticksElapsed = -1;
    private static RichPresence? _lastPresence;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.Tick))]
    internal static void OnTick(Chara __instance)
    {
        if (!__instance.IsPC) {
            return;
        }

        var interval = ErpcConfig.UpdateTicksInterval?.Value ?? 8;
        if (_ticksElapsed >= interval || _ticksElapsed == -1) {
            EnqueueUpdate();
            _ticksElapsed = 0;
        } else {
            ++_ticksElapsed;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.MoveZone))]
    private static void EnqueueUpdate()
    {
        var pc = EClass.pc;
        var presence = new RichPresence {
            Details = pc.NameBraced,
            State = pc.currentZone.GetZoneState(),
            Assets = GeneratePlayerAssets(),
        };

        if (presence.Unchanged(_lastPresence)) {
            return;
        }

        ErpcMod.Session?.Update(presence);
        _lastPresence = presence;
    }

    private static DiscordRPC.Assets GeneratePlayerAssets()
    {
        var pc = EClass.pc;

        var z = pc.currentZone;
        var banner = z.GetBanner();
        var date = EClass.world.date;
        var bannerText = $"{z.InspectName} \ud83d\udcc5 {date.month}/{date.day}, {date.year}";

        var j = pc.job;
        var icon = Jobs.BuiltInJobs.Contains(j.id) ? j.id : "unknown";
        var iconText = $"{pc.race.GetRaceText()} {pc.job.GetJobText()} Lv {EClass.player.totalFeat}";

        return new() {
            LargeImageKey = $"{banner}_banner",
            LargeImageText = bannerText,
            SmallImageKey = $"{icon}_icon",
            SmallImageText = iconText,
        };
    }
}