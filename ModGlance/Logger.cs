namespace Glance;

internal partial class ModGlance
{
    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }
    
    internal static void Error(object payload)
    {
        Instance!.Logger.LogError(payload);
    }
}