using System.ComponentModel;
using Cwl.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.Components;

internal partial class CwlConsole
{
    [ConsoleCommand("cs.eval")]
    [Description("reflex_greedy_args")]
    public static string EvaluateScript(string script)
    {
        return $"{script.ExecuteAsCs()}";
    }

    [ConsoleCommand("cs.eval_file")]
    [Description("reflex_greedy_args")]
    public static string EvaluateScriptFile(string fileName)
    {
        return "";
    }

    [ConsoleCommand("cs.eval_interactive")]
    [Description("reflex_greedy_args")]
    public static string EvaluateScriptInteractive(string fileName)
    {
        return "";
    }
}