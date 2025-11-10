using System;
using System.Runtime.CompilerServices;

namespace ElinTogether.Helper;

// LiteNetLib
public static class FastBitConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void GetBytes<T>(byte[] bytes, int startIndex, T value) where T : unmanaged
    {
        var size = sizeof(T);
        if (bytes.Length < startIndex + size) {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &bytes[startIndex]) {
            *(T*)ptr = value;
        }
    }
}