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

        if (EClass.core.game?.player?.chara is { } pc && pc.GetFlagValue("on_moongate") > 0) {
            if (pc.GetFlagValue("on_exception") > 0) {
                return;
            }

            pc.SetFlagValue("on_exception");

            Dialog.YesNo(
                "exm_ui_moongate_failsafe_desc",
                ReturnLastZone,
                IgnoreException,
                "exm_ui_moongate_failsafe_yes",
                "exm_ui_moongate_failsafe_no");
        }

        return;

        void ReturnLastZone()
        {
            pc.MoveZone(pc.homeZone);
            pc.SetFlagValue("on_moongate", 0);
            pc.SetFlagValue("on_exception", 0);
        }

        void IgnoreException()
        {
            pc.SetFlagValue("on_moongate", 0);
        }
    }
}