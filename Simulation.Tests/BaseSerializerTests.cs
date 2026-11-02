using System.Linq;
using ErasOfWar.Simulation.Base;
using NUnit.Framework;

namespace ErasOfWar.Simulation.Tests
{
    [TestFixture]
    public class BaseSerializerTests
    {
        [Test]
        public void RoundTripRestoresEverything()
        {
            var s = TestData.NewBase();
            var cc = s.Place("command_center", 8, 8).BuildingId;
            s.Upgrade(cc);
            s.Upgrade(cc);                                // CC 3, grid 24
            s.Place("gold_mine", 0, 0);
            s.Place("gold_mine", 2, 0);
            var lab = s.Place("research_lab", 15, 15).BuildingId;
            s.Upgrade(lab);
            s.Place("wall_tile", 23, 23);
            var removed = s.Place("wall_tile", 22, 23).BuildingId;
            s.Remove(removed);

            var json = BaseSerializer.ToJson(s);
            var loaded = BaseSerializer.FromJson(TestData.Catalog, json);

            Assert.That(loaded.Ok, loaded.Error);
            Assert.That(loaded.Dropped, Is.Empty);
            var t = loaded.State;
            Assert.That(t.CcLevel, Is.EqualTo(3));
            Assert.That(t.GridSize, Is.EqualTo(24));
            Assert.That(Describe(t), Is.EqualTo(Describe(s)));
            Assert.That(t.NextId, Is.EqualTo(s.NextId), "ids are not reused after reload");
            Assert.That(t.Get(lab).Level, Is.EqualTo(2));
        }

        [Test]
        public void HighLevelCommandCenterAtFarEdgeLoads()
        {
            var s = TestData.BaseWithCc(5);              // grid 28
            var cc = s.CommandCenter.Id;
            Assert.That(s.Move(cc, 24, 24).Ok);           // only fits in the CC5 grid

            var loaded = BaseSerializer.FromJson(TestData.Catalog, BaseSerializer.ToJson(s));
            Assert.That(loaded.Dropped, Is.Empty);
            Assert.That(loaded.State.CommandCenter.X, Is.EqualTo(24));
        }

        [Test]
        public void BadEntriesAreDroppedNotFatal()
        {
            const string json = @"{
              ""version"": 1, ""next_id"": 10,
              ""buildings"": [
                { ""id"": 1, ""type"": ""command_center"", ""level"": 1, ""x"": 0, ""y"": 0 },
                { ""id"": 2, ""type"": ""gold_mine"", ""level"": 1, ""x"": 1, ""y"": 1 },
                { ""id"": 3, ""type"": ""no_such_thing"", ""level"": 1, ""x"": 10, ""y"": 10 },
                { ""id"": 4, ""type"": ""gold_mine"", ""level"": 99, ""x"": 10, ""y"": 10 },
                { ""id"": 4, ""type"": ""oil_derrick"", ""level"": 1, ""x"": 14, ""y"": 14 },
                { ""id"": 5, ""type"": ""cannon_tower"", ""level"": 1, ""x"": 16, ""y"": 16 }
              ]
            }";

            var r = BaseSerializer.FromJson(TestData.Catalog, json);
            Assert.That(r.Ok);
            Assert.That(r.State.Buildings.Select(b => b.Id), Is.EquivalentTo(new[] { 1, 4 }));
            Assert.That(r.State.Get(4).Level, Is.EqualTo(8), "level clamped to max");
            Assert.That(r.Dropped, Has.Count.EqualTo(4)); // overlap, unknown type, duplicate id, locked cannon
            Assert.That(r.State.NextId, Is.EqualTo(10));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{ \"version\": 99 }")]
        public void UnreadableSaveGivesEmptyBaseAndError(string json)
        {
            var r = BaseSerializer.FromJson(TestData.Catalog, json);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Error, Is.Not.Empty);
            Assert.That(r.State.Buildings, Is.Empty);
        }

        private static string[] Describe(BaseState s) =>
            s.Buildings.OrderBy(b => b.Id).Select(b => $"{b.Id}:{b.TypeId}:L{b.Level}:{b.X},{b.Y}").ToArray();
    }
}
