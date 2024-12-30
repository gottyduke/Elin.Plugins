using System;
using Cwl.LangMod;
using Cwl.Loader;

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
                CwlMod.Warn("cwl_warn_processor".Loc("type", "resolve", ex.Message));
                // noexcept
            }
        }
    }

    internal static void Resolve(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (CwlConfig.AllowProcessors) {
            OnTypeResolve(ref resolved, objectType, ref readType, qualified);
        }
    }
}