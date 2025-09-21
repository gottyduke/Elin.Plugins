using System;
using System.Text;
using IStringBuilderPool = UnityEngine.Pool.ObjectPool<System.Text.StringBuilder>;

namespace Cwl.Helper.String;

public class StringBuilderPool(StringBuilder sb) : IDisposable
{
    private static readonly IStringBuilderPool _stringBuilderPool = new(() => new(), actionOnRelease: sb => sb.Clear());

    public StringBuilder StringBuilder => sb;

    public void Dispose()
    {
        _stringBuilderPool.Release(sb);
    }

    public static StringBuilderPool Get()
    {
        return new(_stringBuilderPool.Get());
    }

    public StringBuilderPool AppendLine(string? message = null)
    {
        sb.AppendLine(message);
        return this;
    }

    public StringBuilderPool Append(string message)
    {
        sb.Append(message);
        return this;
    }

    public override string ToString()
    {
        return sb.ToString();
    }
}