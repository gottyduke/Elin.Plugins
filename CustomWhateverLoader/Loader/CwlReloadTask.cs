namespace Cwl;

internal class CwlReloadTask
{
    internal void Unload()
    {
        CwlMod.SharedHarmony.UnpatchSelf();
    }
}