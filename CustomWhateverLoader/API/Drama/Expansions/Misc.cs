using System.Collections.Generic;
using Cwl.Helper.String;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    /// <summary>
    ///     console_cmd(cmd arg1 arg2 arg3)
    /// </summary>
    public static bool console_cmd(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        string.Join(" ", parameters).ExecuteAsCommand();

        return true;
    }
}