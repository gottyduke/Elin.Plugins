namespace Sparkly.Stats;

internal class ConCarbonated : ConDrunk
{
    public override void SetOwner(Chara conOwner, bool onDeserialize = false)
    {
        base.SetOwner(conOwner, onDeserialize);
        conOwner.isDrunk = false;
    }

    public override void Tick()
    {
        Mod(-1);
    }
}