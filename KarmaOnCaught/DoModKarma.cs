namespace KoC;

internal partial class KocMod
{
    private static bool _skipped;
    
    internal static bool SkipNext()
    {
        var skipped = _skipped;
        _skipped = false;
        return skipped;
    }
    
    internal static void DoModKarma(bool isCrime, Chara? cc, int modifier, bool suspicious = false, int witnesses = 0)
    {
        if (!isCrime) {
            if (!suspicious) {
                return;
            }

            Msg.SetColor("bad");
            Msg.Say(KocLoc.RaiseSuspicion);

            return;
        }

        var doMod = cc switch {
            { IsPC: true } => true,
            { IsPCFaction: true } or { OriginalHostility: >= Hostility.Friend } => true,
            null or { hostility: > Hostility.Enemy } => true,
            _ => false,
        };

        if (!doMod) {
            return;
        }

        Msg.SetColor("bad");
        Msg.Say(KocLoc.CaughtPrompt);
        if (witnesses != 0) {
            Msg.Say(KocLoc.WithWitness(witnesses));
        }

        EClass.player.ModKarma(modifier);
        _skipped = true;
    }
}