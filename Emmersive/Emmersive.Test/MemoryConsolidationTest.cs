using Emmersive.API.ThirdParty;
using Emmersive.Contexts.Memory;

namespace Emmersive.Test;

[TestClass]
public sealed class SummaryParseTest
{
    [TestMethod]
    public void TheReleasedShapeIsANewFact()
    {
        var ops = MemoryConsolidation.Parse("""[{"fact":"Fiama likes fish","importance":3}]""");

        Assert.IsNotNull(ops);
        Assert.AreEqual(1, ops.Count);
        Assert.IsNull(ops[0].Id);
        Assert.AreEqual("Fiama likes fish", ops[0].Fact);
        Assert.AreEqual(3, ops[0].Importance);
    }

    [DataTestMethod]
    [DataRow("""[{"rewrite":2,"fact":"x","importance":4}]""", 2, 4)]
    [DataRow("""[{"rewrite":"#2","fact":"x"}]""", 2, null)]
    [DataRow("""[{"rewrite":"2","fact":"x","importance":"4"}]""", 2, 4)]
    public void ANumberRewritesThatFact(string reply, int id, int? importance)
    {
        var ops = MemoryConsolidation.Parse(reply);

        Assert.IsNotNull(ops);
        Assert.AreEqual(id, ops[0].Id);
        Assert.AreEqual(importance, ops[0].Importance);
    }

    [DataTestMethod]
    [DataRow("[]")]
    [DataRow("""{"facts":[]}""")]
    [DataRow("```json\n[]\n```")]
    public void AnEmptyListMeansNothingToRemember(string reply)
    {
        var ops = MemoryConsolidation.Parse(reply);

        Assert.IsNotNull(ops);
        Assert.AreEqual(0, ops.Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("not json at all")]
    [DataRow("""{"type":"json_object"}""")]
    [DataRow("""[{"speaker":"Fiama","line":"hello"}]""")]
    public void UnusableRepliesAreRejected(string reply)
    {
        Assert.IsNull(MemoryConsolidation.Parse(reply));
    }

    [TestMethod]
    public void WrappedAndFencedRepliesWork()
    {
        var ops = MemoryConsolidation.Parse("```json\n{\"facts\":[{\"fact\":\"a fact\"}]}\n```");

        Assert.IsNotNull(ops);
        Assert.AreEqual("a fact", ops.Single().Fact);
    }

    [TestMethod]
    public void TheModelsOwnNumberingIsNotARewrite()
    {
        var ops = MemoryConsolidation.Parse("""[{"id":1,"fact":"Fiama got a new hat"}]""");

        Assert.IsNotNull(ops);
        Assert.IsNull(ops.Single().Id);
    }

    [DataTestMethod]
    [DataRow("Sure! Here you go:\n[{\"fact\":\"a fact\"}]\nHope this helps.")]
    [DataRow("<think>maybe [this] or {that}</think>\n[{\"fact\":\"a fact\"}]")]
    [DataRow("```json\n[{\"fact\":\"a fact\"}]\n```\nNote: one fact only.")]
    [DataRow("{\"fact\":\"a fact\",\"importance\":2}")]
    [DataRow("[{\"Fact\":\"a fact\"}]")]
    [DataRow("Facts [2] and [3] are covered. New:\n[{\"fact\":\"a fact\"}]")]
    [DataRow("Use [] when nothing is new. Result: [{\"fact\":\"a fact\"}]")]
    [DataRow("{\"result\":{\"fact\":\"a fact\"}}")]
    [DataRow("I rewrote nothing (see {\"rewrite\":3}):\n[{\"fact\":\"a fact\"}]")]
    public void RepliesWrappedInOtherTextStillWork(string reply)
    {
        var ops = MemoryConsolidation.Parse(reply);

        Assert.IsNotNull(ops);
        Assert.AreEqual("a fact", ops.Single().Fact);
    }

    [TestMethod]
    public void OnlyTextFactsAndWholeNumbersCount()
    {
        var ops = MemoryConsolidation.Parse("""[{"fact":{"a":1}},{"fact":"b fact","rewrite":1.6}]""");

        Assert.IsNotNull(ops);
        Assert.AreEqual("b fact", ops.Single().Fact);
        Assert.IsNull(ops.Single().Id);
    }

    [TestMethod]
    public void ItemsWithoutTextAreSkipped()
    {
        var ops = MemoryConsolidation.Parse("""[{"fact":"  "},{"importance":3},{"fact":"b fact"}]""");

        Assert.IsNotNull(ops);
        Assert.AreEqual("b fact", ops.Single().Fact);
    }
}

[TestClass]
public sealed class SummaryApplyTest
{
    private const int Now = 500_000;

    private static CharaMemoryStore Store(params MemoryFact[] facts)
    {
        return new() { Uid = 1, Name = "Fiama", LongTerm = [..facts] };
    }

    private static List<FactChange> Apply(CharaMemoryStore store, params FactOp[] ops)
    {
        return MemoryConsolidation.Apply(store, [..store.LongTerm], ops, Now);
    }

    private static MemoryFact Fact(string text, int importance)
    {
        return new() { Fact = text, Importance = importance };
    }

    [TestMethod]
    public void NewFactsAreStampedWithGameTime()
    {
        var store = Store();

        var changes = Apply(store, new FactOp(null, "Fiama likes fish", 3), new FactOp(null, "Fiama fears thunder", null));

        Assert.AreEqual(2, store.LongTerm.Count);
        Assert.AreEqual(Now, store.LongTerm[0].GameTime);
        Assert.AreEqual(3, store.LongTerm[0].Importance);
        Assert.AreEqual(1, store.LongTerm[1].Importance);
        CollectionAssert.AreEqual(new[] { MemoryFactChange.Added, MemoryFactChange.Added }, changes.Select(c => c.Kind).ToArray());
    }

    [TestMethod]
    public void RepeatingAKnownFactReinforcesIt()
    {
        var fish = new MemoryFact { Fact = "Fiama likes fish", Importance = 2 };
        var store = Store(fish);

        var changes = Apply(store, new FactOp(null, "Fiama likes fish!", 4));

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(1, store.LongTerm.Count);
        Assert.AreEqual(1, fish.RecallCount);
    }

    [TestMethod]
    public void FactsThatOnlyLookAlikeAreBothKept()
    {
        var store = Store(new MemoryFact { Fact = "Fiama likes fish", Importance = 2 });

        Apply(store, new FactOp(null, "Fiama likes figs", 2));

        Assert.AreEqual(2, store.LongTerm.Count);
    }

    [TestMethod]
    public void AnIdRewritesTheNumberedFact()
    {
        var angry = new MemoryFact { Fact = "Fiama is angry at the player", Importance = 3 };
        var store = Store(angry, new MemoryFact { Fact = "Fiama likes fish", Importance = 2 });

        var changes = Apply(store, new FactOp(1, "Fiama forgave the player", 2));

        Assert.AreEqual("Fiama forgave the player", angry.Fact);
        Assert.AreEqual(2, angry.Importance);
        Assert.AreEqual(Now, angry.GameTime);
        var change = changes.Single();
        Assert.AreEqual(MemoryFactChange.Updated, change.Kind);
        Assert.AreSame(angry, change.Fact);
        Assert.AreEqual("Fiama is angry at the player", change.OldFact);
        Assert.AreEqual(3, change.OldImportance);
    }

    [TestMethod]
    public void ARewriteKeepsImportanceWhenNoneIsGiven()
    {
        var angry = new MemoryFact { Fact = "Fiama is angry at the player", Importance = 4 };
        var store = Store(angry);

        Apply(store, new FactOp(1, "Fiama is still angry at the player", null));

        Assert.AreEqual(4, angry.Importance);
    }

    [TestMethod]
    public void RewritesOfFactsRemovedMeanwhileAreDropped()
    {
        var fish = new MemoryFact { Fact = "Fiama likes fish", Importance = 2 };
        var store = Store(new MemoryFact { Fact = "Fiama fears thunder", Importance = 3 }, fish);
        var known = store.LongTerm.ToList();
        store.LongTerm.Remove(fish);

        var changes = MemoryConsolidation.Apply(store, known, [new FactOp(2, "Fiama loves fish", 3)], Now);

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(1, store.LongTerm.Count);
    }

    [TestMethod]
    public void UnknownIdsAreTreatedAsNewFacts()
    {
        var store = Store(new MemoryFact { Fact = "Fiama fears thunder", Importance = 3 });

        var changes = Apply(store, new FactOp(9, "Fiama owns a cat", 2));

        Assert.AreEqual(MemoryFactChange.Added, changes.Single().Kind);
        Assert.AreEqual(2, store.LongTerm.Count);
    }

    [TestMethod]
    public void EachFactIsRewrittenOnce()
    {
        var angry = new MemoryFact { Fact = "Fiama is angry at the player", Importance = 3 };
        var store = Store(angry);

        Apply(store, new FactOp(1, "Fiama forgave the player", 2), new FactOp(1, "Fiama hates the player", 5));

        Assert.AreEqual("Fiama forgave the player", angry.Fact);
    }

    [TestMethod]
    public void EchoingAFactChangesNothing()
    {
        var angry = new MemoryFact { Fact = "Fiama is angry at the player", Importance = 3 };
        var store = Store(angry);

        var changes = Apply(store, new FactOp(1, "Fiama is angry at the player.", 1));

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual("Fiama is angry at the player", angry.Fact);
        Assert.AreEqual(3, angry.Importance);
        Assert.AreEqual(1, angry.RecallCount);
    }

    [TestMethod]
    public void AtMostThreeRewritesPerSummary()
    {
        var store = Store(
            Fact("Fiama likes fish", 2),
            Fact("Fiama fears thunder", 2),
            Fact("Fiama owns a cat", 2),
            Fact("Fiama was born in Palmia", 2),
            Fact("Fiama sings at night", 2));

        var changes = Apply(store,
            new FactOp(1, "Fiama loves fish", 2),
            new FactOp(2, "Fiama no longer fears thunder", 2),
            new FactOp(3, "Fiama lost her cat", 2),
            new FactOp(4, "Fiama left Palmia", 2),
            new FactOp(5, "Fiama stopped singing", 2));

        Assert.AreEqual(3, changes.Count);
        Assert.AreEqual("Fiama was born in Palmia", store.LongTerm[3].Fact);
    }

    [TestMethod]
    public void ARewriteOfAFactEditedMeanwhileIsDropped()
    {
        var angry = new MemoryFact { Fact = "Fiama is angry at the player", Importance = 3 };
        var store = Store(angry);
        List<MemoryFact> known = [angry];
        List<string> knownTexts = [angry.Fact];
        angry.Fact = "Fiama is furious with the player";

        var changes = MemoryConsolidation.Apply(store, known, [new FactOp(1, "Fiama forgave the player", 2)], Now, knownTexts);

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual("Fiama is furious with the player", angry.Fact);
    }

    [DataTestMethod]
    [DataRow("The player promised to bring Fiama a flower from the northern forest",
        "The player never promised to bring Fiama a flower from the northern forest")]
    [DataRow("菲雅玛喜欢在湖边钓鱼", "菲雅玛不喜欢在湖边钓鱼")]
    [DataRow("Fiama can swim across the wide river near the old mill",
        "Fiama can't swim across the wide river near the old mill")]
    [DataRow("菲雅玛答应玩家明天傍晚在维尔尼斯的港口等他一起出海钓鱼", "菲雅玛答应玩家后天傍晚在维尔尼斯的港口等他一起出海钓鱼")]
    public void AChangedFactIsNotADuplicate(string known, string changed)
    {
        var store = Store(Fact(known, 3));

        Apply(store, new FactOp(null, changed, 3));

        Assert.AreEqual(2, store.LongTerm.Count);
    }

    [TestMethod]
    public void AtMostThreeNewFactsPerSummary()
    {
        var store = Store();

        Apply(store,
            new FactOp(null, "Fiama likes fish", 2),
            new FactOp(null, "Fiama fears thunder", 2),
            new FactOp(null, "Fiama owns a cat", 2),
            new FactOp(null, "Fiama was born in Palmia", 2),
            new FactOp(null, "Fiama sings at night", 2));

        Assert.AreEqual(3, store.LongTerm.Count);
    }

    [TestMethod]
    public void BlankTextIsIgnored()
    {
        var angry = new MemoryFact { Fact = "Fiama is angry at the player", Importance = 3 };
        var store = Store(angry);

        var changes = Apply(store, new FactOp(1, "   ", 3), new FactOp(null, "", 3));

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual("Fiama is angry at the player", angry.Fact);
    }
}
