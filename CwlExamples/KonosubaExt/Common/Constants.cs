using System.Collections.Generic;
using KonoExt.Traits;

namespace KonoExt.Common;

internal class Constants
{
    internal const string KonosubaAquaId = "konosuba_aqua";
    internal const string KonosubaChrisId = "konosuba_chris";
    internal const string KonosubaDarknessId = "konosuba_darkenss";
    internal const string KonosubaMeguminId = "konosuba_megumin";
    internal const string KonosubaYunyunId = "konosuba_yunyun";

    internal static readonly Dictionary<string, string> KonosubaAdvs = new() {
        [KonosubaAquaId] = nameof(TraitKonoAdv),
        [KonosubaChrisId] = nameof(TraitKonoAdv), 
        [KonosubaDarknessId] = nameof(TraitKonoAdv),
        [KonosubaMeguminId] = nameof(TraitKonoMegumin),
        [KonosubaYunyunId] = nameof(TraitKonoAdv),
    };

    internal const int SpKonoExplosionId = 58231001;
}