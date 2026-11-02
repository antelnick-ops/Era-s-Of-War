using System.Collections.Generic;
using System.Linq;
using ErasOfWar.Simulation.Data;
using NUnit.Framework;

namespace ErasOfWar.Simulation.Tests
{
    [TestFixture]
    public class GameDataLoaderTests
    {
        private static readonly string[] Nations = { "Germany", "USSR", "USA", "Britain", "Japan" };

        private GameData _data;

        [OneTimeSetUp]
        public void LoadData()
        {
            var dir = GameDataLoader.FindGameDataDirectory(TestContext.CurrentContext.TestDirectory);
            Assert.That(dir, Is.Not.Null, "Could not find game_data/ above the test folder");
            _data = GameDataLoader.LoadFromDirectory(dir);
        }

        // ---- Prompt 01 acceptance

        [Test]
        public void Loads175Units()
        {
            Assert.That(_data.Units, Has.Count.EqualTo(175));
            Assert.That(_data.UnitsById, Has.Count.EqualTo(175), "unit ids must be unique");
        }

        [TestCaseSource(nameof(Nations))]
        public void EachNationHas35Units(string nation)
        {
            Assert.That(_data.UnitsOf(nation).Count(), Is.EqualTo(35));
        }

        [Test]
        public void ConstantsListTheFiveNations()
        {
            Assert.That(_data.Constants.Nations, Is.EquivalentTo(Nations));
            Assert.That(_data.Units.Select(u => u.Nation).Distinct(), Is.EquivalentTo(Nations));
        }

        [Test]
        public void EveryUnitHasPositiveHp()
        {
            var bad = _data.Units.Where(u => u.Hp <= 0).Select(u => u.Id).ToList();
            Assert.That(bad, Is.Empty, "units with HP <= 0");
        }

        [Test]
        public void EveryTechTreeEntryReferencesARealUnit()
        {
            var missing = new List<string>();
            foreach (var t in _data.TechTree.Units)
            {
                if (!_data.UnitsById.ContainsKey(t.UnitId))
                    missing.Add($"unit_id {t.UnitId}");
                if (t.PrerequisiteId != null && !_data.UnitsById.ContainsKey(t.PrerequisiteId))
                    missing.Add($"prerequisite_id {t.PrerequisiteId} (of {t.UnitId})");
            }
            Assert.That(missing, Is.Empty);
            Assert.That(_data.TechTree.Units, Has.Count.EqualTo(175));
        }

        // ---- Extra sanity checks on the other loaded files

        [Test]
        public void TechTreeEntriesMatchRosterNationAndTier()
        {
            foreach (var t in _data.TechTree.Units)
            {
                var u = _data.UnitsById[t.UnitId];
                Assert.That((t.Nation, t.Tier), Is.EqualTo((u.Nation, u.Tier)), t.UnitId);
            }
        }

        [Test]
        public void TierUnlockTableHasFiveTiers()
        {
            Assert.That(_data.TechTree.TierUnlocks, Has.Count.EqualTo(5));
            Assert.That(_data.TechTree.TierRp.Keys, Is.EquivalentTo(new[] { "1", "2", "3", "4", "5" }));
        }

        [Test]
        public void LoadsLeadersAndGenerals()
        {
            Assert.That(_data.Leaders, Has.Count.EqualTo(25));
            Assert.That(_data.Generals, Has.Count.EqualTo(35));
            foreach (var n in Nations)
            {
                Assert.That(_data.Leaders.Count(l => l.Nation == n), Is.EqualTo(5), n);
                Assert.That(_data.Generals.Count(g => g.Nation == n), Is.EqualTo(7), n);
            }
            Assert.That(_data.Generals.All(g => g.Hp > 0 && g.ArmorPct >= 0), Is.True);
        }

        [Test]
        public void EconomyAndConstantsAgreeOnStealRates()
        {
            var e = _data.Economy.StealRates;
            var c = _data.Constants.StealRates;
            Assert.That((e.Gold, e.Oil, e.Steel), Is.EqualTo((0.20, 0.25, 0.15)));
            Assert.That((c.Gold, c.Oil, c.Steel), Is.EqualTo((e.Gold, e.Oil, e.Steel)));
            Assert.That(_data.Economy.Packs, Has.Count.EqualTo(6));
            Assert.That(_data.Economy.NeverSold, Does.Contain("research_points"));
        }

        [Test]
        public void UnknownJsonFieldFailsLoudly()
        {
            var ex = Assert.Throws<GameDataException>(() =>
                GameDataLoader.Load(f => f == GameDataLoader.UnitsFile
                    ? "[{\"id\":\"x\",\"nation\":\"Germany\",\"tier\":1,\"name\":\"X\",\"cls\":\"Light Tank\"," +
                      "\"domain\":\"ground\",\"hp\":1,\"dps\":1,\"gold\":1,\"housing\":1,\"oil\":1,\"typo_field\":5}]"
                    : "{}"));
            Assert.That(ex.Message, Does.Contain(GameDataLoader.UnitsFile));
        }
    }
}
