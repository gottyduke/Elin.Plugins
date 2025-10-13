namespace Cwl;

internal class CwlReloadTask
{
    internal static void Unload()
    {
        CwlMod.SharedHarmony.UnpatchSelf();
    }
}