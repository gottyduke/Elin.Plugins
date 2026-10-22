using System.Collections.Concurrent;
using Emmersive.Contexts.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emmersive.Test;

[TestClass]
public sealed class MemoryCompatTest
{
    private const string ChunkKey =
        "Emmersive.Contexts.Memory.MemoryManager:Emmersive.Contexts.Memory.MemoryManager:emmersive_memory_store";

    private const string LegacyChunk = """
        {
          "$id": "1",
          "Emmersive.Contexts.Memory.MemoryManager:Emmersive.Contexts.Memory.MemoryManager:emmersive_memory_store": {
            "$id": "2",
            "$type": "System.Collections.Concurrent.ConcurrentDictionary`2[[System.Int32, mscorlib],[Emmersive.Contexts.Memory.CharaMemoryStore, Emmersive]], mscorlib",
            "12": {
              "$id": "3",
              "u": 12,
              "id": "fiama",
              "n": "Fiama",
              "stm": [
                { "$id": "4", "t": 3, "ts": "2026-09-01T10:00:00Z", "s": "Player", "c": "Hello Fiama", "i": 1, "sc": 2 },
                { "$id": "5", "t": 5, "ts": "2026-09-01T10:01:00Z", "s": "Fiama", "c": "Welcome back", "i": 1, "sm": true, "gt": 1234567 }
              ],
              "ltm": [
                { "$id": "6", "c": "2026-09-01T10:05:00Z", "lr": "2026-09-02T10:05:00Z", "f": "Fiama likes fish", "i": 3, "rc": 4 },
                { "$id": "7", "c": "2026-09-01T10:06:00Z", "f": "The player saved Fiama from bandits", "i": 5 }
              ],
              "ls": "2026-09-01T10:05:00Z"
            }
          }
        }
        """;

    private static ConcurrentDictionary<int, CharaMemoryStore> LoadLikeTheGame(string chunk)
    {
        var serializer = JsonSerializer.Create(GameIOContext.Settings);
        using var reader = new JsonTextReader(new StringReader(chunk));
        var vars = serializer.Deserialize<Dictionary<string, object>>(reader)!;
        return vars[ChunkKey] switch {
            JToken token => token.ToObject<ConcurrentDictionary<int, CharaMemoryStore>>()!,
            var typed => (ConcurrentDictionary<int, CharaMemoryStore>)typed,
        };
    }

    private static string SaveLikeTheGame(ConcurrentDictionary<int, CharaMemoryStore> stores)
    {
        var serializer = JsonSerializer.CreateDefault(GameIOContext.Settings);
        using var writer = new StringWriter();
        serializer.Serialize(writer, new Dictionary<string, object> { [ChunkKey] = stores });
        return writer.ToString();
    }

    [TestMethod]
    public void LoadsLegacyMemoryUnchanged()
    {
        var store = LoadLikeTheGame(LegacyChunk)[12];

        Assert.AreEqual("Fiama", store.Name);
        Assert.AreEqual(2, store.ShortTerm.Count);
        Assert.AreEqual("Hello Fiama", store.ShortTerm[0].Content);
        Assert.AreEqual(2, store.ShortTerm[0].SentCount);
        Assert.AreEqual(0, store.ShortTerm[0].GameTime);
        Assert.IsTrue(store.ShortTerm[1].Summarized);
        Assert.AreEqual(1234567, store.ShortTerm[1].GameTime);

        Assert.AreEqual(2, store.LongTerm.Count);
        Assert.AreEqual("Fiama likes fish", store.LongTerm[0].Fact);
        Assert.AreEqual(3, store.LongTerm[0].Importance);
        Assert.AreEqual(4, store.LongTerm[0].RecallCount);
        Assert.AreEqual(5, store.LongTerm[1].Importance);
        Assert.IsNull(store.LongTerm[1].LastRecalled);
        Assert.AreEqual(0, store.LongTerm[0].GameTime);
    }

    [TestMethod]
    public void RoundTripKeepsEveryLegacyField()
    {
        var resaved = SaveLikeTheGame(LoadLikeTheGame(LegacyChunk));
        var store = (JObject)JObject.Parse(resaved)[ChunkKey]!["12"]!;

        Assert.AreEqual("Fiama", (string?)store["n"]);
        var fact = (JObject)store["ltm"]![0]!;
        Assert.AreEqual("Fiama likes fish", (string?)fact["f"]);
        Assert.AreEqual(3, (int?)fact["i"]);
        Assert.AreEqual(4, (int?)fact["rc"]);
        Assert.IsNotNull(fact["c"]);
        Assert.IsNotNull(fact["lr"]);

        var entry = (JObject)store["stm"]![0]!;
        Assert.AreEqual("Player", (string?)entry["s"]);
        Assert.AreEqual("Hello Fiama", (string?)entry["c"]);
        Assert.AreEqual(2, (int?)entry["sc"]);
    }

    [TestMethod]
    public void NewFactFieldsPersistAndSessionStateDoesNot()
    {
        var stores = LoadLikeTheGame(LegacyChunk);
        var fact = stores[12].LongTerm[0];
        fact.GameTime = 7654321;
        fact.LastInjectedTick = 42;
        fact.SaidUntilTick = 99;

        var resaved = (JObject)JObject.Parse(SaveLikeTheGame(stores))[ChunkKey]!["12"]!["ltm"]![0]!;

        Assert.AreEqual(7654321, (int?)resaved["gt"]);
        var keys = resaved.Properties().Select(p => p.Name).Where(n => !n.StartsWith('$')).ToHashSet();
        CollectionAssert.AreEquivalent(new[] { "c", "lr", "f", "i", "rc", "gt" }, keys.ToArray());

        var reloaded = LoadLikeTheGame(SaveLikeTheGame(stores))[12].LongTerm[0];
        Assert.AreEqual(7654321, reloaded.GameTime);
        Assert.IsNull(reloaded.LastInjectedTick);
        Assert.IsNull(reloaded.SaidUntilTick);
    }
}
