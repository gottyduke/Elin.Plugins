using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Helper;

public static class AccessorHelper
{
    private static readonly Dictionary<Type, FieldInfo[]> _cachedFields = [];

    extension(object instance)
    {
        public object? GetFieldValue(string fieldName)
        {
            return instance.GetType().GetCachedField(fieldName)?.GetValue(instance);
        }

        public object? GetPropertyValue(string propertyName)
        {
            var flags = AccessTools.all & ~BindingFlags.Static;

            return instance.GetType().GetProperties(flags)
                .Where(p => p.GetGetMethod(true) is not null)
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.Ordinal))?
                .GetValue(instance);
        }

        public (object memberInstance, MemberInfo memberInfo)? GetMemberInfo(string fullMemberName, bool allowMethodCall = true)
        {
            var notation = fullMemberName.IndexOf('.');
            var memberName = notation < 0 ? fullMemberName : fullMemberName[..notation];
            var chained = notation < 0 ? null : fullMemberName[(notation + 1)..];

            var type = instance.GetType();
            var access = AccessTools.all & ~BindingFlags.Static;
            var member = type.GetCachedField(memberName) ??
                         type.GetProperty(memberName, access) as MemberInfo ??
                         (allowMethodCall ? type.GetMethod(memberName, access) : null);

            if (member is not null) {
                return string.IsNullOrEmpty(chained)
                    ? (instance, member)
                    : instance.GetMemberValue(member)?.GetMemberInfo(chained);
            }

            var indexer = memberName.ExtractInBetween('[', ']');
            if (string.IsNullOrEmpty(indexer)) {
                return null;
            }

            object? item = null;
            // check for collection access
            if (instance is IList list && int.TryParse(indexer, out var index) && list.Count > index) {
                item = list[index];
            } else if (instance is IDictionary dictionary) {
                var keyType = dictionary.GetType().GetGenericArguments()[0];
                if (keyType == typeof(int)) {
                    if (!int.TryParse(indexer, out index)) {
                        index = indexer.GetHashCode();
                    }

                    if (dictionary.Contains(index)) {
                        item = dictionary[index];
                    }
                } else if (keyType == typeof(string) && dictionary.Contains(indexer)) {
                    item = dictionary[indexer];
                }
            }

            return item?.GetMemberInfo(chained);
        }

        public object? GetMemberValue(MemberInfo memberInfo)
        {
            return memberInfo switch {
                FieldInfo field => field.GetValue(instance),
                PropertyInfo property => property.GetValue(instance),
                MethodInfo method => method.FastInvoke(instance),
                _ => throw new InvalidOperationException($"MemberInfo '{memberInfo}' is ambiguous"),
            };
        }

        public void SetMemberValue(MemberInfo memberInfo, object? value)
        {
            switch (memberInfo) {
                case FieldInfo field:
                    field.SetValue(instance, value);
                    break;
                case PropertyInfo property:
                    property.SetValue(instance, value);
                    break;
                default:
                    throw new InvalidOperationException($"MemberInfo '{memberInfo}' cannot be set");
            }
        }
    }

    extension(Type type)
    {
        public FieldInfo[] GetCachedFields()
        {
            if (_cachedFields.TryGetValue(type, out var fields)) {
                return fields;
            }

            return _cachedFields[type] = type.GetFields(AccessTools.all & ~BindingFlags.Static);
        }

        public FieldInfo? GetCachedField(string fieldName)
        {
            return type.GetCachedFields().FirstOrDefault(f => f.Name == fieldName);
        }
    }
}