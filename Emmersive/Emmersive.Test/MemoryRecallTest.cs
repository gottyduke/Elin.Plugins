using Emmersive.Contexts.Memory;

namespace Emmersive.Test;

[TestClass]
public sealed class MemoryRelevanceTest
{
    private static float Relevance(string fact, string line, out bool triggered, params string[] ignore)
    {
        return MemoryRelevance.Relevance(
            MemoryRelevance.Tokenize(fact),
            [MemoryRelevance.Tokenize(line)],
            MemoryRelevance.IgnoreTokens(ignore),
            out triggered);
    }

    private static float Relevance(string fact, string line, params string[] ignore)
    {
        return Relevance(fact, line, out _, ignore);
    }

    [TestMethod]
    public void LatinWordsMatchAcrossCaseAndPlurals()
    {
        var score = Relevance("The player promised to bring Fiama a flower", "Did you like the FLOWERS I brought?", out var triggered, "Fiama");

        Assert.AreEqual(0.5f, score, 1e-4f);
        Assert.IsTrue(triggered);
    }

    [TestMethod]
    public void CjkBigramsCarryTheMatch()
    {
        var score = Relevance("玩家答应给菲雅玛带一朵花", "那朵花呢", out var triggered, "菲雅玛");

        Assert.AreEqual(1f, score, 1e-4f);
        Assert.IsTrue(triggered);
    }

    [TestMethod]
    public void SharedCharactersAloneDoNotTrigger()
    {
        var score = Relevance("菲雅玛喜欢花", "花钱太多了", out var triggered, "菲雅玛");

        Assert.AreEqual(0f, score);
        Assert.IsFalse(triggered);
    }

    [TestMethod]
    public void IgnoredNamesNeverCountAsAMatch()
    {
        Assert.AreEqual(0f, Relevance("Fiama likes fish", "Fiama, over here!", "Fiama"));
        Assert.AreEqual(0f, Relevance("The player gave Fiama a sword", "Player!", "Fiama"));
        Assert.AreEqual(0f, Relevance("玩家送给菲雅玛一把剑", "玩家", "菲雅玛"));
    }

    [TestMethod]
    public void NamesOnlyMatchWholeWords()
    {
        Assert.AreEqual(0f, Relevance("Ash keeps washing the dishes", "Your sword is flashing", "Ash"));
    }

    [TestMethod]
    public void CommonWordsDoNotTrigger()
    {
        Assert.AreEqual(0f, Relevance("Fiama is a good cook", "Good to see you!", "Fiama"));
        Assert.AreEqual(0f, Relevance("Ashland doesn't know the way", "I don't know.", "Ashland"));
        Assert.AreEqual(0f, Relevance("Fiama loves the sea", "I love it here", "Fiama"));
    }

    [TestMethod]
    public void FunctionWordsDoNotTrigger()
    {
        Assert.AreEqual(0f, Relevance("菲雅玛是一个厨师", "他是一个好人", "菲雅玛"));
        Assert.AreEqual(0f, Relevance("フィアマは魚が好きです", "そうですね"));
        Assert.AreEqual(0f, Relevance("피아마는 생선을 좋아합니다", "감사합니다"));
    }

    [TestMethod]
    public void UnrelatedLinesScoreZero()
    {
        Assert.AreEqual(0f, Relevance("Fiama likes fish", "The weather is nice today", "Fiama"));
        Assert.AreEqual(0f, Relevance("菲雅玛喜欢鱼", "今天天气不错", "菲雅玛"));
    }

    [TestMethod]
    public void BestLineWins()
    {
        var score = MemoryRelevance.Relevance(
            MemoryRelevance.Tokenize("Fiama likes fish"),
            [MemoryRelevance.Tokenize("The weather is nice today"), MemoryRelevance.Tokenize("Fish again?")],
            MemoryRelevance.IgnoreTokens(["Fiama"]),
            out var triggered);

        Assert.AreEqual(1f, score, 1e-4f);
        Assert.IsTrue(triggered);
    }

    [TestMethod]
    public void EmptyInputsScoreZero()
    {
        Assert.AreEqual(0f, Relevance("", "fish"));
        Assert.AreEqual(0f, Relevance("Fiama likes fish", ""));
        Assert.AreEqual(0f, MemoryRelevance.Relevance(MemoryRelevance.Tokenize("fish"), [], [], out _));
    }
}

[TestClass]
public sealed class MemoryRecallTest
{
    private const int Now = 1_000_000;

    private static MemoryFact Fact(string text, int importance, int gameTime = Now)
    {
        return new() { Fact = text, Importance = importance, GameTime = gameTime };
    }

    private static CharaMemoryStore Store(params MemoryFact[] facts)
    {
        return new() { Uid = 1, Name = "Fiama", LongTerm = [..facts] };
    }

    private static RecallRequest Request(int tick, int max = 3, int cooldown = 3, string? line = null, string? playerLine = null)
    {
        List<HashSet<string>> lines = [..new[] { line, playerLine }.OfType<string>().Select(MemoryRelevance.Tokenize)];
        List<HashSet<string>> playerLines = playerLine is null ? [] : [MemoryRelevance.Tokenize(playerLine)];
        return new(lines, MemoryRelevance.IgnoreTokens(["Fiama"]), tick, max, cooldown, Now, playerLines);
    }

    private static string[] Recall(CharaMemoryStore store, RecallRequest request)
    {
        return [..store.RecallLtm(request).Select(f => f.Fact)];
    }

    [TestMethod]
    public void RelevantFactsComeFirstAndFillersAreCapped()
    {
        var store = Store(
            Fact("Fiama was born in Palmia", 5),
            Fact("Fiama likes fish", 2),
            Fact("Fiama fears thunder", 3),
            Fact("Fiama owns a red scarf", 1));

        var recalled = Recall(store, Request(1, max: 5, line: "Want some fish?"));

        CollectionAssert.AreEqual(new[] { "Fiama likes fish", "Fiama was born in Palmia", "Fiama fears thunder" }, recalled);
    }

    [TestMethod]
    public void MaxCapsTheTotal()
    {
        var store = Store(Fact("Fiama likes fish", 2), Fact("Fiama was born in Palmia", 5));

        CollectionAssert.AreEqual(new[] { "Fiama likes fish" }, Recall(store, Request(1, max: 1, line: "fish?")));
        Assert.AreEqual(0, Recall(store, Request(2, max: 0, line: "fish?")).Length);
    }

    [TestMethod]
    public void InjectedFactsSitOutTheCooldown()
    {
        var palmia = Fact("Fiama was born in Palmia", 4);
        var store = Store(palmia, Fact("Fiama fears thunder", 3), Fact("Fiama owns a red scarf", 1));

        CollectionAssert.Contains(Recall(store, Request(10)), palmia.Fact);
        Assert.AreEqual(10, palmia.LastInjectedTick);

        CollectionAssert.DoesNotContain(Recall(store, Request(11)), palmia.Fact);
        CollectionAssert.DoesNotContain(Recall(store, Request(13)), palmia.Fact);
        CollectionAssert.Contains(Recall(store, Request(14)), palmia.Fact);
    }

    [TestMethod]
    public void ASingleCoreFactSkipsEveryOtherRequest()
    {
        var palmia = Fact("Fiama was born in Palmia", 5);
        var store = Store(palmia, Fact("Fiama fears thunder", 3), Fact("Fiama owns a red scarf", 1));

        CollectionAssert.Contains(Recall(store, Request(10)), palmia.Fact);
        CollectionAssert.DoesNotContain(Recall(store, Request(11)), palmia.Fact);
        CollectionAssert.Contains(Recall(store, Request(12)), palmia.Fact);
        CollectionAssert.DoesNotContain(Recall(store, Request(13)), palmia.Fact);

        palmia.SaidUntilTick = 20;
        CollectionAssert.DoesNotContain(Recall(store, Request(14)), palmia.Fact);
    }

    [TestMethod]
    public void CoreFactsTakeTurns()
    {
        var palmia = Fact("Fiama was born in Palmia", 5);
        var bandits = Fact("The player saved Fiama from bandits", 5);
        var store = Store(palmia, bandits, Fact("Fiama fears thunder", 3));

        var seen = Enumerable.Range(10, 6).Select(tick => Recall(store, Request(tick))).ToList();

        Assert.IsTrue(seen.Count(r => r.Contains(palmia.Fact)) is >= 2 and < 6);
        Assert.IsTrue(seen.Count(r => r.Contains(bandits.Fact)) is >= 2 and < 6);
        Assert.IsFalse(seen.Any(r => r.Contains(palmia.Fact) && r.Contains(bandits.Fact) && r != seen[0]));
    }

    [TestMethod]
    public void RelevanceBypassesTheCooldown()
    {
        var fish = Fact("Fiama likes fish", 2);
        var store = Store(fish);
        fish.LastInjectedTick = 9;

        CollectionAssert.Contains(Recall(store, Request(10, line: "fish for dinner")), fish.Fact);
    }

    [TestMethod]
    public void FactsTheNpcJustSaidStayOutEvenWhenRelevant()
    {
        var fish = Fact("Fiama likes fish", 2);
        var store = Store(fish);
        fish.SaidUntilTick = 12;

        Assert.AreEqual(0, Recall(store, Request(10, line: "fish for dinner")).Length);
        Assert.AreEqual(0, Recall(store, Request(12, line: "fish for dinner")).Length);
        CollectionAssert.Contains(Recall(store, Request(13, line: "fish for dinner")), fish.Fact);
    }

    [TestMethod]
    public void ThePlayerAskingOverridesTheSaidCooldown()
    {
        var fish = Fact("Fiama likes fish", 2);
        var store = Store(fish);
        fish.SaidUntilTick = 12;

        CollectionAssert.Contains(Recall(store, Request(10, playerLine: "Do you still like fish?")), fish.Fact);
    }

    [TestMethod]
    public void TheFirstRequestSeesEveryFact()
    {
        var store = Store(Fact("Fiama fears thunder", 3));

        CollectionAssert.Contains(Recall(store, Request(0)), "Fiama fears thunder");
    }

    [TestMethod]
    public void OnlyRelevantRecallsCountAsRecalled()
    {
        var fish = Fact("Fiama likes fish", 2);
        var palmia = Fact("Fiama was born in Palmia", 5);
        var store = Store(fish, palmia);

        Recall(store, Request(1, line: "fish?"));

        Assert.AreEqual(1, fish.RecallCount);
        Assert.IsNotNull(fish.LastRecalled);
        Assert.AreEqual(0, palmia.RecallCount);
        Assert.IsNull(palmia.LastRecalled);
        Assert.AreEqual(1, palmia.LastInjectedTick);
    }

    [TestMethod]
    public void NewerFactsWinAmongEquals()
    {
        var store = Store(
            Fact("Fiama lost her ring", 3, Now - 60 * 1440),
            Fact("Fiama found a kitten", 3, Now - 60),
            Fact("Fiama sang at the inn", 3));
        store.LongTerm[2].GameTime = 0;

        var recalled = Recall(store, Request(1, max: 5));

        CollectionAssert.AreEqual(new[] { "Fiama found a kitten", "Fiama sang at the inn" }, recalled);
    }

    [TestMethod]
    public void BlankFactsAreSkippedButKept()
    {
        var store = Store(Fact("", 3), Fact("   ", 3), Fact("Fiama likes fish", 2));

        CollectionAssert.AreEqual(new[] { "Fiama likes fish" }, Recall(store, Request(1)));
        Assert.AreEqual(3, store.LongTerm.Count);
    }

    [TestMethod]
    public void SayingAFactStartsItsSaidCooldown()
    {
        var fish = Fact("Fiama likes fish", 2);
        var palmia = Fact("Fiama was born in Palmia", 5);
        var store = Store(fish, palmia);

        store.MarkSaid("I still like fish, you know.", ["Fiama"], tick: 20, cooldown: 9);

        Assert.AreEqual(29, fish.SaidUntilTick);
        Assert.IsNull(palmia.SaidUntilTick);
    }

    [DataTestMethod]
    [DataRow("The guild master owes Fiama a lot of money", "Guild business again...")]
    [DataRow("Fiama likes fishing at the lake", "The lake looks calm today.")]
    [DataRow("菲雅玛喜欢在湖边钓鱼", "今天湖边好安静。")]
    [DataRow("Fiama owes the player money, lots of money.", "Money!")]
    public void BrushingAgainstAFactIsNotSayingIt(string text, string line)
    {
        var fact = Fact(text, 3);
        var store = Store(fact);

        store.MarkSaid(line, ["Fiama", "菲雅玛"], tick: 20, cooldown: 9);

        Assert.IsNull(fact.SaidUntilTick);
    }

    [DataTestMethod]
    [DataRow("菲雅玛喜欢鱼", "我还是喜欢鱼")]
    [DataRow("玩家送给菲雅玛一把魔法剑。", "你送我的这把魔法剑，我一直带着。")]
    [DataRow("菲雅玛欠玩家一百金币。", "那一百金币我一定会还你的。")]
    public void SayingACjkFactInOtherWordsCountsAsSaid(string text, string line)
    {
        var fact = Fact(text, 2);
        var store = Store(fact);

        store.MarkSaid(line, ["菲雅玛"], tick: 20, cooldown: 9);

        Assert.AreEqual(29, fact.SaidUntilTick);
    }

    [TestMethod]
    public void OldImportantFactsOutliveNewTrivia()
    {
        var store = Store();
        for (var i = 0; i < 6; i++) {
            store.LongTerm.Add(new() {
                Fact = $"Fiama made vow number {i}",
                Importance = 5,
                Created = DateTime.UtcNow.AddDays(-30 + i),
            });
        }

        var trivia = new MemoryFact { Fact = "Fiama ate bread", Importance = 1 };
        store.LongTerm.Add(trivia);

        var forgotten = store.EvictLtm(6, DateTime.UtcNow);

        CollectionAssert.AreEqual(new[] { trivia }, forgotten);
    }

    [TestMethod]
    public void EvictionNoLongerFavorsInflatedRecallCounts()
    {
        var legacy = new MemoryFact {
            Fact = "Fiama likes fish",
            Importance = 3,
            RecallCount = 40,
            Created = DateTime.UtcNow.AddDays(-60),
            LastRecalled = DateTime.UtcNow.AddDays(-30),
        };
        var fresh = new MemoryFact { Fact = "Fiama found a kitten", Importance = 3 };
        var store = Store(legacy, fresh);

        var forgotten = store.EvictLtm(1, DateTime.UtcNow);

        CollectionAssert.AreEqual(new[] { legacy }, forgotten);
        CollectionAssert.AreEqual(new[] { fresh }, store.LongTerm);
    }
}

[TestClass]
public sealed class ShortTermMemoryTest
{
    private static CharaMemoryStore Store()
    {
        return new() { Uid = 1, Name = "Fiama" };
    }

    private static string[] Contents(CharaMemoryStore store)
    {
        return [..store.ShortTerm.Select(e => e.Content)];
    }

    [TestMethod]
    public void NpcLinesDoNotResetTheSentCount()
    {
        var store = Store();
        store.AddStm("Player", "Hello", 1, 100, 12);
        store.ShortTerm[0].SentCount = 2;

        store.AddStm("Fiama", "Welcome back", 2, 101, 12);

        Assert.AreEqual(2, store.ShortTerm[0].SentCount);
    }

    [TestMethod]
    public void PlayerLinesResetTheSentCount()
    {
        var store = Store();
        store.AddStm("Fiama", "Welcome back", 1, 100, 12);
        store.ShortTerm[0].SentCount = 2;

        store.AddStm("Player", "Hello", 2, 101, 12);

        Assert.AreEqual(0, store.ShortTerm[0].SentCount);
    }

    [TestMethod]
    public void NpcBarksRepeatedVerbatimAreStoredOnce()
    {
        var store = Store();
        store.AddStm("Fiama", "Fresh fish for sale!", 1, 100, 12);
        store.AddStm("Player", "Hi", 2, 101, 12);
        store.AddStm("Fiama", "fresh fish, for sale!!", 3, 102, 12);

        CollectionAssert.AreEqual(new[] { "Fresh fish for sale!", "Hi" }, Contents(store));
    }

    [TestMethod]
    public void PlayerLinesAreNeverDropped()
    {
        var store = Store();
        store.AddStm("Player", "Yes.", 1, 100, 12);
        store.AddStm("Fiama", "Shall we go?", 2, 101, 12);
        store.AddStm("Player", "Yes.", 3, 102, 12);

        CollectionAssert.AreEqual(new[] { "Yes.", "Shall we go?", "Yes." }, Contents(store));
    }

    [TestMethod]
    public void NpcLinesThatOnlyLookAlikeAreKept()
    {
        var store = Store();
        store.AddStm("Fiama", "Let's go north", 1, 100, 12);
        store.AddStm("Fiama", "Let's go south", 2, 101, 12);
        store.AddStm("Fiama", "I agree.", 3, 102, 12);
        store.AddStm("Fiama", "I agree. Let's go to Palmia tomorrow.", 4, 103, 12);

        Assert.AreEqual(4, store.ShortTerm.Count);
    }

    [TestMethod]
    public void RepeatsBeyondTheLookbackAreStoredAgain()
    {
        var store = Store();
        store.AddStm("Fiama", "Fresh fish for sale!", 1, 100, 12);
        string[] chatter = ["Hi there", "Where is the inn?", "I found a sword", "Let's go north"];
        for (var i = 0; i < chatter.Length; i++) {
            store.AddStm("Player", chatter[i], 2 + i, 101 + i, 12);
        }

        store.AddStm("Fiama", "Fresh fish for sale!", 9, 200, 12);

        Assert.AreEqual(6, store.ShortTerm.Count);
        Assert.AreEqual(200, store.ShortTerm[^1].GameTime);
    }

    [TestMethod]
    public void OldestEntriesAreDroppedFirst()
    {
        var store = Store();
        string[] lines = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot"];
        for (var i = 0; i < lines.Length; i++) {
            store.AddStm("Player", lines[i], i, 100 + i, 4);
        }

        CollectionAssert.AreEqual(new[] { "charlie", "delta", "echo", "foxtrot" }, Contents(store));
    }

    [TestMethod]
    public void RecentStmSkipsEntriesSentTooOften()
    {
        var store = Store();
        store.AddStm("Player", "Good morning", 1, 100, 12);
        store.AddStm("Player", "Where to?", 2, 101, 12);
        store.ShortTerm[0].SentCount = 2;

        var recent = store.GetRecentStm(5, 2, "Fiama", 110);

        CollectionAssert.AreEqual(new[] { "Where to?" }, recent.Select(e => e.Content).ToArray());
    }

    [TestMethod]
    public void TheOwnersLatestLinesStayVisible()
    {
        var store = Store();
        store.AddStm("Fiama", "Old news", 1, 100, 12);
        store.AddStm("Fiama", "Fresh fish", 2, 101, 12);
        store.AddStm("Fiama", "Rain is coming", 3, 102, 12);
        store.AddStm("Player", "Hi", 4, 103, 12);
        foreach (var entry in store.ShortTerm) {
            entry.SentCount = 5;
        }

        var recent = store.GetRecentStm(5, 2, "Fiama", 110);

        CollectionAssert.AreEqual(new[] { "Fresh fish", "Rain is coming" }, recent.Select(e => e.Content).ToArray());
    }

    [TestMethod]
    public void TheOwnersOldLinesStillRetire()
    {
        var store = Store();
        store.AddStm("Fiama", "Fresh fish", 1, 100, 12);
        store.ShortTerm[0].SentCount = 5;

        Assert.AreEqual(0, store.GetRecentStm(5, 2, "Fiama", 100 + 6 * 60 + 1).Count);
    }

    [TestMethod]
    public void RecentRepeatsAreFoundWithinAFewGameHours()
    {
        var store = Store();
        store.AddStm("Fiama", "The fish here is fresh today", 1, 100, 12);

        Assert.IsTrue(store.HasRecentRepeat("The fish here is fresh today!", 6, 150));
        Assert.IsFalse(store.HasRecentRepeat("The fish here is fresh today!", 6, 100 + 181));
        store.AddStm("Fiama", "Yes.", 2, 150, 12);
        Assert.IsFalse(store.HasRecentRepeat("Yes.", 6, 151));
    }

    [TestMethod]
    public void EntriesWrittenInOneInstantKeepTheirOrder()
    {
        var instant = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);

        var first = CharaMemoryStore.NextEntryTime(instant);
        var second = CharaMemoryStore.NextEntryTime(instant);
        var third = CharaMemoryStore.NextEntryTime(instant);

        Assert.IsTrue(first < second && second < third);
    }

    [TestMethod]
    public void ThePlayerHasNoAlwaysVisibleLines()
    {
        var store = Store();
        store.AddStm("Player", "Where is the inn?", 1, 100, 12);
        store.ShortTerm[0].SentCount = 5;

        Assert.AreEqual(0, store.GetRecentStm(5, 2, null, 110).Count);
    }
}
