using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class TypeDeltaBase : ElinDeltaBase
{
    [Key(0)]
    public required string TypeName { get; init; }

    [Key(1)]
    public List<int> ChangedIndices { get; set; } = [];

    [Key(2)]
    public List<byte[]> ChangedValues { get; set; } = [];

    public override void Apply(ElinNetBase net)
    {
    }

    private static class TypeDeltaCache
    {
        private const BindingFlags TypeFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.DeclaredOnly;

        private static readonly ConcurrentDictionary<Type, MemberInfo[]> _cache = [];

        public static MemberInfo[] GetMembers(Type type)
        {
            return _cache.GetOrAdd(type, GetAllMembers);

            static MemberInfo[] GetAllMembers(Type? type)
            {
                if (type == null) {
                    return [];
                }

                return GetBaseTypes(type)
                    .Reverse()
                    .SelectMany(t => t.GetMembers(TypeFlags))
                    .Where(m => m is FieldInfo { IsInitOnly: false } ||
                                (m is PropertyInfo { CanRead: true, CanWrite: true } p && p.GetIndexParameters().Length == 0))
                    .ToArray();

                static IEnumerable<Type> GetBaseTypes(Type t)
                {
                    for (var cur = t; cur != null; cur = cur.BaseType) {
                        yield return cur;
                    }
                }
            }
        }
    }
}