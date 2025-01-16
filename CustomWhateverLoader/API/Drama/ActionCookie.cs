using System.Collections.Generic;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public record ActionCookie(DramaManager Dm, Dictionary<string, string> Line);
}