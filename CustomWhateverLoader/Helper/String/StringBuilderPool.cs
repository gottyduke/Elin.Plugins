using System;
using System.Collections.Concurrent;
using System.Text;

namespace Cwl.Helper.String;

public class StringBuilderPool(StringBuilder sb) : IDisposable
{
    private static readonly ConcurrentStack<StringBuilderPool> _stringBuilderPool = [];
    public StringBuilder StringBuilder => sb;

    public void Dispose()
    {
        sb.Clear();
        _stringBuilderPool.Push(this);
    }

    public static StringBuilderPool Get()
    {
        return _stringBuilderPool.TryPop(out var stringBuilderPool)
            ? stringBuilderPool
            : new(new());
    }

    public StringBuilderPool AppendLine(string? message = null)
    {
        sb.AppendLine(message);
        return this;
    }

    public StringBuilderPool Append(string? message)
    {
        sb.Append(message);
        return this;
    }

    public StringBuilderPool Append(char singleChar)
    {
        sb.Append(singleChar);
        return this;
    }

    public StringBuilderPool Append(char[] message)
    {
        sb.Append(message);
        return this;
    }

    public StringBuilderPool Clear()
    {
        sb.Clear();
        return this;
    }

    public override string ToString()
    {
        return sb.ToString();
    }
}