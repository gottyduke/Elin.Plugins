using System;

namespace Cwl.Helper.String;

public static class GetEnv
{
    extension(string env)
    {
        public string EnvVar => Environment.GetEnvironmentVariable(env) ?? "";
    }
}