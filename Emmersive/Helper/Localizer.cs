using Cwl.LangMod;

namespace Emmersive.Helper;

public static class Localizer
{
    extension(string input)
    {
        public bool TryLocalize(out string result)
        {
            if (Lang.General.map.TryGetValue($"em_{input}", out var row)) {
                result = row.Loc();
                if (!result.IsEmpty()) {
                    return true;
                }
            }

            result = input;
            return false;
        }
    }
}