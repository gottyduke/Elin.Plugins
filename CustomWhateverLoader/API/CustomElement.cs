using System.Collections;
using System.Collections.Generic;
using Cwl.Loader;
using Cwl.Loader.Patches;
using MethodTimer;

namespace Cwl.API;

public class CustomElement : Element
{
    internal static readonly Dictionary<int, SourceElement.Row> Managed = [];

    public static IEnumerable<SourceElement.Row> All => Managed.Values;

    public bool AutoGainOnLoad { get; set; } = true;

    // credits to 105gun
    [Time]
    internal static IEnumerator GainAbilityOnLoad()
    {
        if (!SafeSceneInitPatch.SafeToCreate) {
            yield break;
        }

        foreach (var element in All) {
            if (!element.tag.Contains("addEleOnLoad") ||
                player?.chara?.HasElement(element.id) is not false) {
                continue;
            }

            player.chara.GainAbility(element.id);
            CwlMod.Log($"added element {element.id} ");
        }
    }
}