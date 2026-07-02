using System;

namespace Exm.Helper;

public static class GetEnv
{
    extension(string env)
    {
        public string EnvVar => Environment.GetEnvironmentVariable(env) ?? "";
    }
}