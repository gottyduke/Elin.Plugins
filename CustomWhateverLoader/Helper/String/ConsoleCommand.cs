using System;
using System.Reflection;
using ReflexCLI;

namespace Cwl.Helper.String;

public static class ConsoleCommand
{
    extension(string commandStr)
    {
        public string ExecuteAsCommand(bool msg = false)
        {
            object? result;
            try {
                result = Utils.ExecuteCommand(ref commandStr);
            } catch (CommandException ex) {
                result = ex.Message;
            } catch (TargetInvocationException ex2) {
                result = "Command generated internal exception: " + ex2.InnerException?.Message;
            } catch (Exception ex3) {
                result = ex3.Message;
            }

            if (result is null or "") {
                return "";
            }

            if (msg) {
                CwlMod.Popup<ReflexConsole>(result.ToString());
            }

            return result.ToString();
        }
    }
}