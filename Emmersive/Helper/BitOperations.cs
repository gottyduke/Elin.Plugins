namespace Emmersive.Helper;

public class BitOperations
{
    public static int PopCount(int i)
    {
        var u = (uint)i;

        u -= (u >> 1) & 0x55555555;
        u = (u & 0x33333333) + ((u >> 2) & 0x33333333);
        u = (u + (u >> 4)) & 0x0f0f0f0f;
        u += u >> 8;
        u += u >> 16;

        return (int)(u & 0x3f);
    }

    public static int PopCount(long i)
    {
        var u = (ulong)i;

        u -= (u >> 1) & 0x5555555555555555ul;
        u = (u & 0x3333333333333333ul) + ((u >> 2) & 0x3333333333333333ul);
        u = (u + (u >> 4)) & 0x0f0f0f0f0f0f0f0ful;
        u += u >> 8;
        u += u >> 16;
        u += u >> 32;

        return (int)(u & 0x7f);
    }
}