using ReflexCLI.Attributes;

namespace Cwl.API.Drama;

[ConsoleCommandClassCustomizer("cwl.dm")]
public partial class DramaExpansion : DramaOutcome
{
    public static ActionCookie? Cookie { get; internal set; }
}