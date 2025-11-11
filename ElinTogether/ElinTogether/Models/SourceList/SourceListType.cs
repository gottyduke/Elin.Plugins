namespace ElinTogether.Models;

public enum SourceListType : byte
{
    None = 0,
    Reserved = 1 << 7,

    // mod assemblies
    Assembly,

    // source data
    Card,
    Zone,
    Element,
    Job,
    Race,
    Material,
    Religion,
    Quest,
    Stat,

    // runtime data
    AiAct,

    //
    All,
}