using Cwl.ThirdParty;

namespace Cwl.Loader;

internal sealed partial class CwlMod
{
    internal static void Log(object payload)
    {
        Instance!.Logger.LogInfo(payload);
    }

    internal static void Log(object payload, string category)
    {
        Instance!.Logger.LogInfo($"[{category}] {payload}");
    }

    internal static void Debug(object payload)
    {
        if (!CwlConfig.Logging.Verbose?.Value is true) {
            return;
        }

        Instance!.Logger.LogInfo(payload);
    }

    internal static void Warn(object payload)
    {
        Instance!.Logger.LogWarning(payload);
        Glance.Dispatch(payload);
    }

    internal static void Error(object payload)
    {
        Instance!.Logger.LogError(payload);
        Glance.Dispatch(payload);
    }
}