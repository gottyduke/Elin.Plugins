using System;

namespace TextureExpand.Rules;

internal sealed class RuleJudge
{
    private readonly string _conflict;
    private readonly Func<Chara, bool> _predicate;

    internal RuleJudge(string matchPhrase, RuleKeyword keyword, Func<Chara, bool> predicate)
    {
        MatchPhrase = matchPhrase;
        Keyword = keyword;
        _predicate = predicate;
        _conflict = matchPhrase.StartsWith("#not-")
            ? "#" + matchPhrase[5..]
            : "#not-" + matchPhrase[1..];
    }

    internal string MatchPhrase { get; }

    internal RuleKeyword Keyword { get; }

    internal bool IsMatch(Chara chara)
    {
        return _predicate(chara);
    }

    internal bool IsConflict(string matchPhrase)
    {
        return _conflict == matchPhrase;
    }
}