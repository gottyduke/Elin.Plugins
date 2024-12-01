namespace KoC;

internal partial class KocMod
{
    internal static void DoModKarma(bool isCrime, Chara? cc, int modifier, bool suspicious = false, int witnesses = 0)
    {
        if (!isCrime) {
            return;
        }

        var doMod = cc switch {
            { IsPC: true } => true,
            { IsPCFaction: true } or { OriginalHostility: >= Hostility.Friend } => true,
            null or { hostility: > Hostility.Enemy } => true,
            _ => false,
        };

        if (doMod) {
            Msg.SetColor("bad");
            Msg.Say(KocLoc.CaughtPrompt);
            if (witnesses != 0) {
                Msg.Say(KocLoc.WithWitness(witnesses));
            }

            EClass.player.ModKarma(modifier);
        } else if (suspicious) {
            Msg.Say(KocLoc.RaiseSuspicion);
        }
    }
}