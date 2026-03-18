using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper.String;

public static class MethodInfoDetail
{
    extension(MethodBase methodInfo)
    {
        public string GetDetail(bool full = true)
        {
            var decl = methodInfo.DeclaringType;
            if (decl is null) {
                return methodInfo.Name;
            }

            var name = full ? decl.FullName! : decl.Name;

            if (decl.IsGenericType) {
                var genericArgs = string.Join(",", decl.GenericTypeArguments.Select(t => t.FullName ?? t.Name));
                name += $"<{genericArgs}>";
            }

            if (MethodCompatibility.CheckedCalls.GetValueOrDefault(methodInfo)) {
                name = "cwl_ui_invalid_patch".lang() + name;
            }

            return $"{name}.{methodInfo.Name}";
        }


        private string GetAssemblyQualifiedDetail(bool full, bool colorize, bool includeParams)
        {
            var asmName = TypeQualifier.GetMappedAssemblyName(methodInfo.Module.Assembly);
            if (colorize) {
                asmName = asmName.TagColor(0x7676a7);
            }

            var methodDetail = methodInfo.GetDetail(full);
            if (includeParams) {
                methodDetail += $" ({methodInfo.GetParameters().Join()})";
            }

            return $"{asmName}::{methodDetail}";
        }

        public string GetAssemblyDetail(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, false, false);
        }

        public string GetAssemblyDetailColor(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, true, false);
        }

        public string GetAssemblyDetailParams(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, false, true);
        }

        public string GetAssemblyDetailParamsColor(bool full = true)
        {
            return methodInfo.GetAssemblyQualifiedDetail(full, true, true);
        }
    }
}