using System;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl.API.Processors;

/// <summary>
///     event raised when json deserialization <b>failed</b> resolving types
/// </summary>
public class TypeResolver
{
    // objectType should be readonly, only mutate the readType and mark it as resolved
    public delegate void TypeResolve(ref bool resolved, Type objectType, ref Type readType, string qualified);

    private static event TypeResolve OnTypeResolve = delegate { };

    public static void Add(TypeResolve resolver)
    {
        OnTypeResolve += Process;
        return;

        void Process(ref bool resolved, Type objectType, ref Type readType, string qualified)
        {
            try {
                resolver(ref resolved, objectType, ref readType, qualified);
            } catch (Exception ex) {
                CwlMod.Warn<TypeResolver>("cwl_warn_processor".Loc("type", "resolve", ex.Message));
                // noexcept
            }
        }
    }

    internal static void Resolve(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        OnTypeResolve(ref resolved, objectType, ref readType, qualified);
    }

    /// <summary>
    ///     New stuff added in EA 23.76 Nightly
    ///     Actually it doesn't really work
    /// </summary>
    [Time]
    internal static void RegisterFallbacks()
    {
        /*
        foreach (var (declared, fallback) in TypeQualifier.Declared) {
            // assembly name is unused. NOA!!!!!
            var asm = declared.Assembly.FullName;
            var aqn = fallback.AssemblyQualifiedName;
            ModUtil.RegisterSerializedTypeFallback(asm, declared.Name, aqn);
            ModUtil.RegisterSerializedTypeFallback(asm, declared.FullName, aqn);
        }
        /**/
    }
}