using System;
using Cwl.LangMod;
using Cwl.Loader;

namespace Cwl.API.Processors;

/// <summary>
///     event raised when game instantiates a Trait
/// </summary>
public class TraitTransformer
{
    // Trait should be immutable, only transform the trait name
    public delegate void TraitTransform(ref string traitName, Card traitOwner);

    private static event TraitTransform OnTraitTransform = delegate { };

    public static void Add(TraitTransform transformer)
    {
        OnTraitTransform += Process;
        return;

        void Process(ref string traitName, Card traitOwner)
        {
            try {
                transformer(ref traitName, traitOwner);
            } catch (Exception ex) {
                CwlMod.Warn("cwl_warn_processor".Loc("trait", "transform", ex.Message));
                // noexcept
            }
        }
    }

    internal static void Transform(ref string traitName, Card traitOwner)
    {
        if (CwlConfig.AllowProcessors) {
            OnTraitTransform(ref traitName, traitOwner);
        }
    }
}