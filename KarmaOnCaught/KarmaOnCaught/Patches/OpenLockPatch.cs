using HarmonyLib;

namespace KarmaOnCaught.Patches;

[HarmonyPatch]
internal class OpenLockPatch
{
    private static KocConfig.Patch Config => KocConfig.Managed["Lockpick"];

    internal static bool Prepare()
    {
        return Config.Enabled!.Value;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Trait), nameof(Trait.OnLockOpen))]
    internal static bool OnLockOpenCrime(Trait __instance, Chara cc)
    {
        if (!cc.IsPC || !__instance.owner.isLostProperty) {
            return true;
        }

        var chest = __instance.owner;
        var lockLv = chest.c_lockLv;

        chest.c_lockLv = 0;
        chest.isLostProperty = false;
        if (chest.c_lockedHard) {
            chest.c_lockedHard = false;
            chest.c_priceAdd = 0;
        }

        var difficulty = 0f;
        var detection = Config.DetectionRadius!.Value;
        var mod = Config.DifficultyModifier!.Value;
        var skill = (cc.Evalue(SKILL.lockpicking) + cc.DEX) / 2f;

        var witnesses = chest.pos.ListWitnesses(cc, detection).Count;
        var caught = chest.pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
            var los = w.CanSee(cc) ? 50 : 0;
            var perception = w.PER * (75 + los) / 100;

            var randomCost = EClass.rndf(perception + lockLv + mod);
            difficulty += randomCost;

            return randomCost > skill;
        });

        var suspicion = difficulty / skill;
        KocMod.DoModKarma(caught, cc, -8, suspicion >= 0.65f, witnesses);

        if (chest.GetBool(CINT.isFiamaChest)) {
            Steam.GetAchievement(ID_Achievement.FIAMA_CHEST);
        }

        return false;
    }
}