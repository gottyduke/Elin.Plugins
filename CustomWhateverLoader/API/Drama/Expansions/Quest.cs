using System.Collections.Generic;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    /// <summary>
    ///     enable, disable, accept, complete, fail
    /// </summary>
    public static bool set_quest_state(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        return true;
    }

    /// <summary>
    ///     title, tracker, detail, detail_full, progress, reward, talk_progress, talk_complete
    /// </summary>
    public static bool set_quest_text(DramaManager dm, Dictionary<string, string> line, params string[] parameters)
    {
        return true;
    }
}