using Cwl.Helper.Runtime.Exceptions;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public static void RequireParameters(string[] parameters, int count)
    {
        if (parameters.Length < count) {
            throw new DramaActionArgumentException(parameters);
        }
    }

    public static void RequireParameters(string[] parameters, out string arg1)
    {
        RequireParameters(parameters, 1);
        arg1 = parameters[0];
    }

    public static void RequireParameters(string[] parameters, out string arg1, out string arg2)
    {
        RequireParameters(parameters, 2);
        arg1 = parameters[0];
        arg2 = parameters[1];
    }

    public static void RequireParameters(string[] parameters, out string arg1, out string arg2, out string arg3)
    {
        RequireParameters(parameters, 3);
        arg1 = parameters[0];
        arg2 = parameters[1];
        arg3 = parameters[2];
    }
}