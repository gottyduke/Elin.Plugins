namespace ElinTogether.Models;

public enum SourceListType : byte
{
    None = 0,
    Reserved = 1 << 7,

    //
    Assembly,
    Card,
    Zone,
    Element,
    Job,
    Race,
    Material,
    Religion,
    Quest,
    Stats,

    //
    All,
}