using System;
using System.Collections.Generic;
using System.Linq;

namespace ElinTogether.Net.Steam;

internal class SteamNetTypeRegistry
{
    private static readonly Dictionary<uint, Type> _hashToType = [];
    private static readonly Dictionary<Type, uint> _typeToHash = [];

    /// <summary>
    ///     Must be registered first
    /// </summary>
    public static uint GetHash(Type type)
    {
        return _typeToHash[type];
    }

    /// <summary>
    ///     Auto registers hash to type mapping
    /// </summary>
    public static uint GetHash<T>()
    {
        return TypeHash<T>.Hash;
    }

    public static Type? Resolve(uint hash)
    {
        return _hashToType.GetValueOrDefault(hash);
    }

    private static class TypeHash<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly uint Hash;

        static TypeHash()
        {
            var typeName = typeof(T).ToString();
            unchecked {
                Hash = typeName.Aggregate(2166136261u, (current, c) => (current ^ c) * 16777619u);
            }

            _hashToType[Hash] = typeof(T);
            _typeToHash[typeof(T)] = Hash;
        }
    }
}