namespace KoC;

internal partial class KocMod
{
    internal static void DoModKarma(bool isCrime, Chara? cc, int modifier)
    {
        if (!isCrime) {
            return;
        }

        EClass.pc.Say(KocLoc.CaughtPrompt);

        // pc
        if (cc?.IsPC ?? false) {
            EClass.player.ModKarma(modifier);
            return;
        }
        
        // target
        if (cc is not null && (cc.IsPCFaction || cc.OriginalHostility >= Hostility.Friend)) {
            EClass.player.ModKarma(modifier);
        } else if (cc is null || cc.hostility > Hostility.Enemy) {
            EClass.player.ModKarma(modifier);
        }
    }
}