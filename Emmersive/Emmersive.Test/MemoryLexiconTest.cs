using Emmersive.Contexts.Memory;

namespace Emmersive.Test;

[TestClass]
public sealed class MemoryLexiconTest
{
    private const string Defaults = """
        {"player_words":["player"],"stop_words":["the"],"hours_ago":" ({0}h ago)","significant_chars":"0123456789"}
        """;

    [TestMethod]
    public void AnOverrideOnlyReplacesWhatItNames()
    {
        var lexicon = MemoryLexicon.Merge(Defaults, """{"stop_words":["fish"]}""");

        CollectionAssert.AreEquivalent(new[] { "fish" }, lexicon.StopWords.ToArray());
        CollectionAssert.AreEqual(new[] { "player" }, lexicon.PlayerWords);
        Assert.AreEqual(" ({0}h ago)", lexicon.HoursAgo);
    }

    [TestMethod]
    public void NullsInAnOverrideAreIgnored()
    {
        var lexicon = MemoryLexicon.Merge(Defaults, """{"stop_words":null,"hours_ago":null,"significant_chars":null}""");

        CollectionAssert.AreEquivalent(new[] { "the" }, lexicon.StopWords.ToArray());
        Assert.AreEqual(" ({0}h ago)", lexicon.HoursAgo);
        Assert.AreEqual("0123456789", lexicon.SignificantChars);
    }
}
