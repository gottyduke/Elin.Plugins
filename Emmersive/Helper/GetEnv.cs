using System;

namespace Emmersive.Helper;

public static class GetEnv
{
    extension(string env)
    {
        public string EnvVar => Environment.GetEnvironmentVariable(env) ?? "";
    }
}