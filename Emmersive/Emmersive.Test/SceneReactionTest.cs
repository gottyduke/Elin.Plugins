using Emmersive.API.Plugins;

namespace Emmersive.Test;

[TestClass]
public sealed class SceneReactionTest
{
    /// <summary>
    ///     Providers pinned to a plain <c>json_object</c> response format cannot answer with a
    ///     bare array, so they wrap it under a key of their own choosing and sometimes echo the
    ///     requested format back. Every shape below was captured from a real provider response.
    /// </summary>
    [DataTestMethod]
    // bare array, what the system prompt asks for
    [DataRow("""[{"uid":10,"text":"a","duration":1.5,"delay":0},{"uid":11,"text":"b","duration":1.5,"delay":1.5}]""", 2)]
    // openai json_schema
    [DataRow("""{"items":[{"uid":10,"text":"a","duration":1.5,"delay":0}]}""", 1)]
    // deepseek json_object, note the echoed response format ahead of the payload
    [DataRow("""{"type":"json_object","reactions":[{"uid":10,"text":"a","duration":1.5,"delay":0}]}""", 1)]
    [DataRow("""{"type":"json_object","characters":[{"uid":10,"text":"a","duration":1.5,"delay":0}]}""", 1)]
    // arbitrary wrapper key
    [DataRow("""{"whatever":[{"uid":10,"text":"a","duration":1.5,"delay":0}]}""", 1)]
    // nested wrapper
    [DataRow("""{"result":{"reactions":[{"uid":10,"text":"a","duration":1.5,"delay":0}]}}""", 1)]
    // single item, no array at all
    [DataRow("""{"uid":10,"text":"a","duration":1.5,"delay":0}""", 1)]
    // markdown fenced
    [DataRow("```json\n[{\"uid\":10,\"text\":\"a\",\"duration\":1.5,\"delay\":0}]\n```", 1)]
    // quoted numbers
    [DataRow("""[{"uid":"10","text":"a","duration":"1.5","delay":"0"}]""", 1)]
    // partial items are dropped, not defaulted into an unplayable uid 0
    [DataRow("""[{"uid":10,"text":"a"},{"text":"no uid"},{"uid":11},{"uid":12,"text":"  "}]""", 1)]
    public void ParsesProviderShapes(string payload, int expected)
    {
        var reactions = SceneDirector.TryParseReactions(payload);

        Assert.IsNotNull(reactions);
        Assert.AreEqual(expected, reactions.Length);
        Assert.IsTrue(reactions.All(r => r.uid != 0 && !string.IsNullOrWhiteSpace(r.text)));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not json at all")]
    [DataRow("[]")]
    [DataRow("{}")]
    // the echoed response format on its own must not pass as a reaction
    [DataRow("""{"type":"json_object"}""")]
    [DataRow("""{"reactions":[]}""")]
    [DataRow("""[{"speaker":"someone","line":"hello"}]""")]
    public void RejectsUnplayablePayloads(string payload)
    {
        Assert.IsNull(SceneDirector.TryParseReactions(payload));
    }

    [DataTestMethod]
    [DataRow("""[{"uid":10,"text":"a","duration":1.5,"delay":2}]""", 1.5f, 2f)]
    // missing timings fall back to the bubble defaults instead of 0
    [DataRow("""[{"uid":10,"text":"a"}]""", 2.5f, 0f)]
    // negative timings would schedule in the past
    [DataRow("""[{"uid":10,"text":"a","duration":-1,"delay":-5}]""", 0f, 0f)]
    public void ReadsTimings(string payload, float duration, float delay)
    {
        var reactions = SceneDirector.TryParseReactions(payload);

        Assert.IsNotNull(reactions);
        Assert.AreEqual(1, reactions.Length);
        Assert.AreEqual(duration, reactions[0].duration);
        Assert.AreEqual(delay, reactions[0].delay);
    }
}

[TestClass]
public sealed class SceneScheduleTest
{
    private const int PlayerUid = 1;

    private static List<(SceneReaction Reaction, float Delay)> Schedule(float minimalGap, params (int Uid, float Duration, float Delay)[] reactions)
    {
        return SceneDirector.Schedule(
            reactions.Select(r => new SceneReaction { uid = r.Uid, text = "x", duration = r.Duration, delay = r.Delay }),
            PlayerUid,
            minimalGap,
            _ => true);
    }

    [TestMethod]
    public void KeepsTheModelsOwnPacingWhenItIsWideEnough()
    {
        var schedule = Schedule(0.5f, (10, 1.5f, 0f), (11, 1.5f, 1.5f), (12, 1.5f, 3f));

        CollectionAssert.AreEqual(new[] { 0f, 1.5f, 3f }, schedule.Select(s => s.Delay).ToArray());
    }

    [TestMethod]
    public void StaggersReactionsThatAllShareOneDelay()
    {
        // the flooding case, every reaction comes back with delay 0
        var schedule = Schedule(0.5f, (10, 1.5f, 0f), (11, 1.5f, 0f), (12, 1.5f, 0f), (13, 1.5f, 0f));

        CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f, 1.5f }, schedule.Select(s => s.Delay).ToArray());
    }

    [TestMethod]
    public void GapOfZeroLeavesTheDelaysAlone()
    {
        var schedule = Schedule(0f, (10, 1.5f, 0f), (11, 1.5f, 0f));

        CollectionAssert.AreEqual(new[] { 0f, 0f }, schedule.Select(s => s.Delay).ToArray());
    }

    [TestMethod]
    public void ClosesTheGapLeftByASkippedPlayerLine()
    {
        var schedule = Schedule(0f, (PlayerUid, 1.5f, 0f), (11, 1.5f, 1.5f));

        Assert.AreEqual(1, schedule.Count);
        Assert.AreEqual(11, schedule[0].Reaction.uid);
        Assert.AreEqual(0f, schedule[0].Delay);
    }

    [TestMethod]
    public void NegativeOffsetsNeverScheduleInThePast()
    {
        var schedule = Schedule(0f, (PlayerUid, 5f, 0f), (11, 1.5f, 1.5f));

        Assert.AreEqual(0f, schedule[0].Delay);
    }

    [TestMethod]
    public void DropsUnreachableCharas()
    {
        var reactions = new[] {
            new SceneReaction { uid = 10, text = "x", duration = 1.5f, delay = 0f },
            new SceneReaction { uid = 11, text = "x", duration = 1.5f, delay = 1.5f },
        };

        var schedule = SceneDirector.Schedule(reactions, PlayerUid, 0.5f, uid => uid == 11);

        Assert.AreEqual(1, schedule.Count);
        Assert.AreEqual(11, schedule[0].Reaction.uid);
        // the only surviving reaction keeps its own offset, no gap is applied to the first one
        Assert.AreEqual(1.5f, schedule[0].Delay);
    }
}
