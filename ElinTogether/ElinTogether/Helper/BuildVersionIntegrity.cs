using System;

namespace ElinTogether.Helper;

internal class BuildVersionIntegrity
{
    public static long VersionStringToLong(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 4) {
            throw new ArgumentException("Version must have 4 components: major.minor.patch.build");
        }

        var major = byte.Parse(parts[0]);
        var minor = byte.Parse(parts[1]);
        var patch = byte.Parse(parts[2]);
        var build = uint.Parse(parts[3]);

        long result = 0;
        result |= (long)major << 56;
        result |= (long)minor << 48;
        result |= (long)patch << 40;
        result |= build;
        return result;
    }

    public static string LongToVersionString(long value)
    {
        var major = (byte)((value >> 56) & 0xFF);
        var minor = (byte)((value >> 48) & 0xFF);
        var patch = (byte)((value >> 40) & 0xFF);
        var build = (uint)(value & 0xFFFFFFFFFF);

        return $"{major}.{minor}.{patch}.{build}";
    }
}