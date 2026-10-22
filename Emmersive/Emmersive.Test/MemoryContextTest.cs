using Emmersive.API.Plugins;
using Emmersive.Contexts.Memory;

namespace Emmersive.Test;

[TestClass]
public sealed class MemoryTranscriptTest
{
    [TestMethod]
    public void MergesListenersAndOrdersByGameTime()
    {
        var transcript = MemoryContext.BuildTranscript([
            new("Player", "Where is the inn?", 100, 2),
            new("Fiama", "Over there.", 101, 3),
            new("Player", "Where is the inn?", 100, 2),
            new("Ashland", "Follow me", 99, 1),
        ], 110);

        CollectionAssert.AreEqual(new[] {
            "[Ashland]: Follow me",
            "[Player]: Where is the inn?",
            "[Fiama]: Over there.",
        }, transcript);
    }

    [TestMethod]
    public void TalksWithinOneGameMinuteKeepTheirOrder()
    {
        var transcript = MemoryContext.BuildTranscript([
            new("Fiama", "Down the road.", 100, 20),
            new("Fiama", "Cheaper than Palmia.", 100, 40),
            new("Player", "Where is the inn?", 100, 10),
            new("Player", "Is it cheap?", 100, 30),
        ], 100);

        CollectionAssert.AreEqual(new[] {
            "[Player]: Where is the inn?",
            "[Fiama]: Down the road.",
            "[Player]: Is it cheap?",
            "[Fiama]: Cheaper than Palmia.",
        }, transcript);
    }

    [TestMethod]
    public void OlderTalksShowHowLongAgo()
    {
        const int now = 10_000;
        var transcript = MemoryContext.BuildTranscript([
            new("Fiama", "fresh line", now - 30, 0),
            new("Ashland", "earlier today", now - 180, 0),
            new("Quru", "two days back", now - 2 * 1440 - 5, 0),
            new("Loytel", "from an old save", 0, 0),
        ], now);

        CollectionAssert.AreEqual(new[] {
            "[Loytel]: from an old save",
            "[Quru] (2d ago): two days back",
            "[Ashland] (3h ago): earlier today",
            "[Fiama]: fresh line",
        }, transcript);
    }

    [TestMethod]
    public void KeepsTheFullerOfTwoSimilarLines()
    {
        var transcript = MemoryContext.BuildTranscript([
            new("Fiama", "Fresh fish", 100, 0),
            new("Fiama", "Fresh fish for sale", 101, 0),
        ], 110);

        CollectionAssert.AreEqual(new[] { "[Fiama]: Fresh fish for sale" }, transcript);
    }
}

[TestClass]
public sealed class MemoryTalkSelectionTest
{
    private const int Now = 1000;

    private static CharaMemoryStore Store(string name, params (string Speaker, string Content)[] lines)
    {
        var store = new CharaMemoryStore { Uid = name.GetHashCode(), Name = name };
        foreach (var (speaker, content) in lines) {
            store.AddStm(speaker, content, 0, Now, 12);
        }

        return store;
    }

    private static TalkSelection Select(IReadOnlyList<(CharaMemoryStore? Store, string? Owner, string LogName)> stores,
                                        IReadOnlyList<(string? Speaker, string Text)> triggers,
                                        Dictionary<string, List<TalkLine>>? logs = null)
    {
        return MemoryContext.SelectTalks(stores, triggers, logs ?? [], 5, 2, Now);
    }

    [TestMethod]
    public void TheTriggerIsLeftOutEvenWhenStoredInParts()
    {
        var player = Store("Gotty", ("Player", "*waves*"), ("Player", "Hi, how's the shop?"));
        var fiama = Store("Fiama", ("Player", "*waves*"), ("Player", "Hi, how's the shop?"), ("Fiama", "Welcome!"));

        var selection = Select(
            [(fiama, "Fiama", "Fiama"), (player, null, "Gotty")],
            [("Player", "*waves* Hi, how's the shop?")]);

        CollectionAssert.AreEqual(new[] { "Welcome!" }, selection.Talks.Select(t => t.Content).ToArray());
        Assert.AreEqual(4, selection.Covered.Count);
    }

    [TestMethod]
    public void OldShortLinesAreNotSwallowedByALongTrigger()
    {
        var player = Store("Gotty", ("Player", "No."), ("Player", "Is this the way to the north gate? No idea."));
        player.ShortTerm[0].Seen = true;

        var selection = Select([(player, null, "Gotty")], [("Player", "Is this the way to the north gate? No idea.")]);

        CollectionAssert.AreEqual(new[] { "No." }, selection.Talks.Select(t => t.Content).ToArray());
    }

    [TestMethod]
    public void OnlyLinesNotSeenBeforeCountAsNews()
    {
        var fiama = Store("Fiama", ("Player", "Got any fish?"), ("Fiama", "Fresh today!"));
        fiama.ShortTerm[0].Seen = true;

        var selection = Select([(fiama, "Fiama", "Fiama")], []);

        CollectionAssert.AreEqual(new[] { "Fresh today!" }, selection.Unseen.Select(u => u.Content).ToArray());
        Assert.AreEqual(2, selection.Sent.Count);
    }

    [TestMethod]
    public void TheGameLogFillsRoomWithLinesNotInMemory()
    {
        var fiama = Store("Fiama", ("Fiama", "Fresh today!"));
        var logs = new Dictionary<string, List<TalkLine>> {
            ["Fiama"] = [new("Fiama", "Fresh today!", Now, 0), new("Fiama", "Mind the step.", Now, 0)],
            ["Guard"] = [new("Guard", "Move along.", Now, 0)],
        };

        var selection = Select([(fiama, "Fiama", "Fiama"), (null, "Guard", "Guard")], [("Guard", "Move along.")], logs);

        CollectionAssert.AreEquivalent(new[] { "Fresh today!", "Mind the step." }, selection.Talks.Select(t => t.Content).ToArray());
    }

    [TestMethod]
    public void RecallLinesLeaveOutTheNpcsOwnWordsAndMarkThePlayers()
    {
        var (lines, playerLines) = MemoryContext.RecallLines(
            "Fiama",
            [("Player", "Any fish today?"), ("Fiama", "Fish!"), (null, "Mind the fish")],
            [("Ashland", "I hate fish"), ("Fiama", "Fish again")]);

        CollectionAssert.AreEquivalent(new[] { "Any fish today?", "Mind the fish", "I hate fish" }, lines);
        CollectionAssert.AreEqual(new[] { "Any fish today?" }, playerLines);
    }
}

[TestClass]
public sealed class TriggerFreshnessTest
{
    [TestMethod]
    public void ThePlayerAskingAgainIsAlwaysNews()
    {
        var manager = new MemoryManager();
        List<(string? Speaker, string Text)> triggers = [("Player", "Any fish today?"), ("Fiama", "Fresh fish!")];

        MemoryContext.FreshTriggers(triggers, manager, 1, 6);
        var again = MemoryContext.FreshTriggers(triggers, manager, 2, 6);

        CollectionAssert.AreEqual(new[] { "Any fish today?" }, again.Select(t => t.Text).ToArray());
    }

    [TestMethod]
    public void ABarkHeardAgainSoonIsNoLongerNews()
    {
        var manager = new MemoryManager();

        Assert.IsTrue(manager.IsNewTrigger("Fresh fish for sale!", 1, 6));
        Assert.IsFalse(manager.IsNewTrigger("fresh fish, for sale", 3, 6));
        Assert.IsFalse(manager.IsNewTrigger("Fresh fish for sale!", 8, 6));
        Assert.IsTrue(manager.IsNewTrigger("Fresh fish for sale!", 15, 6));
    }
}

[TestClass]
public sealed class RecentLineGuardTest
{
    [TestMethod]
    public void BlocksASpeakerRepeatingItself()
    {
        var guard = new RecentLineGuard();
        guard.Add(1, "The fish here is fresh today", 0f);

        Assert.IsTrue(guard.IsRepeat(1, "The fish here is fresh today!", false, 500f));
    }

    [TestMethod]
    public void BlocksParrotingSomeoneElse()
    {
        var guard = new RecentLineGuard();
        guard.Add(0, "Have you seen my sword?", 10f);

        Assert.IsTrue(guard.IsRepeat(2, "Have you seen my sword?", false, 15f));
    }

    [TestMethod]
    public void ShortLinesMayEchoAcrossSpeakers()
    {
        var guard = new RecentLineGuard();
        guard.Add(0, "Good morning.", 10f);
        guard.Add(2, "Careful!", 10f);
        guard.Add(3, "你好", 10f);

        Assert.IsFalse(guard.IsRepeat(2, "Good morning!", false, 11f));
        Assert.IsFalse(guard.IsRepeat(4, "Careful!", false, 11f));
        Assert.IsFalse(guard.IsRepeat(4, "你好", false, 11f));
    }

    [TestMethod]
    public void GesturesMayEchoAcrossSpeakers()
    {
        var guard = new RecentLineGuard();
        guard.Add(2, "*nods*", 10f);

        Assert.IsFalse(guard.IsRepeat(3, "*nods*", true, 11f));
        Assert.IsTrue(guard.IsRepeat(2, "*nods*", true, 11f));
    }

    [TestMethod]
    public void ShortLinesFromOneSpeakerReturnAfterAMinute()
    {
        var guard = new RecentLineGuard();
        guard.Add(5, "Woof!", 0f);

        Assert.IsTrue(guard.IsRepeat(5, "Woof!", false, 30f));
        Assert.IsFalse(guard.IsRepeat(5, "Woof!", false, RecentLineGuard.ShortLineSeconds + 1f));
    }

    [TestMethod]
    public void OtherSpeakersAreForgottenAfterTheWindow()
    {
        var guard = new RecentLineGuard();
        guard.Add(0, "Have you seen my sword?", 10f);

        Assert.IsFalse(guard.IsRepeat(2, "Have you seen my sword?", false, 10f + RecentLineGuard.CrossSpeakerSeconds + 1f));
    }

    [TestMethod]
    public void ChatterAroundDoesNotPushOutOwnLines()
    {
        var guard = new RecentLineGuard();
        guard.Add(1, "The fish here is fresh today", 0f);
        for (var i = 0; i < 40; i++) {
            guard.Add(2 + i, $"vanilla bark number {i}", 1f);
        }

        Assert.IsTrue(guard.IsRepeat(1, "The fish here is fresh today", false, 2f));
    }

    [TestMethod]
    public void OwnLinesFallOutAfterEnoughNewOnes()
    {
        var guard = new RecentLineGuard();
        guard.Add(1, "The fish here is fresh today", 0f);
        string[] more = ["alpha line here", "bravo line here", "charlie line here", "delta line here",
            "echo line here", "foxtrot line here", "golf line here", "hotel line here"];
        foreach (var line in more) {
            guard.Add(1, line, 1f);
        }

        Assert.IsFalse(guard.IsRepeat(1, "The fish here is fresh today", false, 2f));
    }

    [TestMethod]
    public void UnrelatedLinesPass()
    {
        var guard = new RecentLineGuard();
        guard.Add(1, "Good morning to you all", 0f);

        Assert.IsFalse(guard.IsRepeat(1, "The bridge is out again", false, 1f));
        Assert.IsFalse(guard.IsRepeat(2, "The bridge is out again", false, 1f));
    }
}

[TestClass]
public sealed class LineRepeatTest
{
    [DataTestMethod]
    [DataRow("The fish here is fresh today", "the fish here is fresh today!!")]
    [DataRow("今天的鱼很新鲜", "今天的鱼很新鲜啊")]
    [DataRow("这家店的酒真是好喝啊！", "这家店的酒真是好喝呀！")]
    [DataRow("*nods*", "*nods*")]
    public void SameLinesAreRepeats(string a, string b)
    {
        Assert.IsTrue(MemoryRelevance.IsNearDuplicate(a, b));
    }

    [DataTestMethod]
    [DataRow("Let's go north", "Let's go south")]
    [DataRow("I agree.", "I agree. Let's go to Palmia tomorrow.")]
    [DataRow("Yes", "Yes, but later")]
    [DataRow("我饿了", "我也饿了")]
    [DataRow("Do you want to go fishing?", "Do you want to go hunting?")]
    [DataRow("I will wait for you at the inn.", "I will not wait for you at the inn.")]
    [DataRow("我会在旅馆等你", "我不会在旅馆等你")]
    public void LinesThatOnlyLookAlikeAreNot(string a, string b)
    {
        Assert.IsFalse(MemoryRelevance.IsNearDuplicate(a, b));
    }
}
