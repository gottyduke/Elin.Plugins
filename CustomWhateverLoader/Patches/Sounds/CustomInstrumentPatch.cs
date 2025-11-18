using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

// oh, whither art thou gone, 105gun??
[HarmonyPatch]
internal class CustomInstrumentPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AI_PlayMusic), nameof(AI_PlayMusic.Run), MethodType.Enumerator)]
    internal static IEnumerable<CodeInstruction> OnSetIdSongIl(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        return cm
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, "idSong"),
                new OperandContains(OpCodes.Callvirt, nameof(Dictionary<,>.ContainsKey)))
            .EnsureValid("play music set idSong")
            .Advance(-1)
            .SetOpcodeAndAdvance(OpCodes.Ldflda)
            .InsertAndAdvance(
                new(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate(SetCustomIdSong))
            .InstructionEnumeration();
    }

    private static string SetCustomIdSong(ref string idSong, AI_PlayMusic ai)
    {
        if (idSong != "violin_chaconne") {
            return idSong;
        }

        var id = ai.tool.id;
        if (id is "instrument_violin" or "panty") {
            return idSong;
        }

        if (SoundManager.current.GetData($"Instrument/{id}") != null) {
            idSong = id;
        }

        return idSong;
    }
}