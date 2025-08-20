using System;
using System.Reflection;
using Cwl.Helper.Unity;
using ReflexCLI;

namespace Cwl.Helper.String;

public static class ConsoleCommand
{
    extension(string commandStr)
    {
        public void ExecuteAsCommand()
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
                return;
            }

            using var progress = ProgressIndicator.CreateProgressScoped(() => new(result.ToString()), 3f);
        }
    }
}