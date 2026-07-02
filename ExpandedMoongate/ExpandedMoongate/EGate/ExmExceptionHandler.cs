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

        if (EClass.core.game?.player?.chara is { } pc && pc.GetBool("on_moongate")) {
            if (pc.GetBool("on_exception")) {
                return;
            }

            pc.SetBool("on_exception", true);

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
            pc.SetBool("on_moongate", false);
            pc.SetBool("on_exception", false);
        }

        void IgnoreException()
        {
            pc.SetBool("on_moongate", false);
        }
    }
}