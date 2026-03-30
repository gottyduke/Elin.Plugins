using Cwl.Helper.Extensions;
using UnityEngine;

namespace Exm;

internal class ExmExceptionHandler
{
    internal static void SetupExceptionHook()
    {
        Application.logMessageReceived += ExceptionHandler;
    }

    private static void ExceptionHandler(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Exception) {
            return;
        }

        if (EClass.core.game?.player?.chara is { currentZone: Zone_User moongate } pc) {
            if (moongate.mapInt.ContainsKey("on_exception")) {
                return;
            }

            moongate.mapInt.Set("on_exception", 1);

            Dialog.YesNo(
                "exm_ui_moongate_failsafe_desc",
                () => pc.MoveZone(pc.homeZone),
                () => moongate.mapInt.Remove("on_exception"),
                "exm_ui_moongate_failsafe_yes",
                "exm_ui_moongate_failsafe_no");
        }
    }
}