using System.Linq;
using ErasOfWar.Simulation.Base;
using NUnit.Framework;

namespace ErasOfWar.Simulation.Tests
{
    [TestFixture]
    public class BuildingCatalogTests
    {
        private static BuildingCatalog C => TestData.Catalog;

        [Test]
        public void HasEveryPlaceableBuildingFromTheFootprintTable()
        {
            // 34 footprint rows minus the fixed perimeter wall
            Assert.That(C.Buildings, Has.Count.EqualTo(33));
            Assert.That(C.Buildings.Select(b => b.Name), Does.Not.Contain("Perimeter Wall Section"));
            Assert.That(C.CommandCenter.Id, Is.EqualTo("command_center"));
            Assert.That(C.WallTile.Id, Is.EqualTo("wall_tile"));
        }

        [Test]
        public void OnlyKnownDataWarnings()
        {
            // The one known mismatch: WTBB_07 says "Patriot SAM Battery", WTBB_04 count table says "Patriot SAM".
            Assert.That(C.Warnings, Has.Count.EqualTo(1));
            Assert.That(C.Warnings[0], Does.Contain("Patriot SAM"));
        }

        [TestCase("command_center", 4, 4)]
        [TestCase("gold_mine", 2, 2)]
        [TestCase("barracks", 3, 3)]
        [TestCase("machine_gun_nest", 1, 1)]
        [TestCase("wall_tile", 1, 1)]
        [TestCase("thaad_battery", 4, 4)]
        public void FootprintsComeFromData(string id, int w, int h)
        {
            var d = C.Get(id);
            Assert.That((d.Width, d.Height), Is.EqualTo((w, h)));
        }

        [Test]
        public void GridSizeAndWallLimitsByCc()
        {
            var grids = Enumerable.Range(1, 10).Select(C.GridSize).ToArray();
            Assert.That(grids, Is.EqualTo(new[] { 20, 22, 24, 26, 28, 30, 33, 35, 38, 40 }));
            var walls = Enumerable.Range(1, 10).Select(C.WallTileLimit).ToArray();
            Assert.That(walls, Is.EqualTo(new[] { 50, 55, 65, 75, 90, 105, 125, 145, 170, 200 }));
            Assert.That(C.WallTile.MaxCountAt(1), Is.EqualTo(50));
        }

        [Test]
        public void GridSizesAgreeBetweenConstantsAndLayoutWorkbook()
        {
            var rows = TestData.Data.Table("tables_07_BaseLayout.json").Rows("Grid Size by CC Level");
            foreach (var r in rows)
            {
                var cc = int.Parse(((string)r["CC Level"]).Replace("CC", "").Trim());
                var side = int.Parse(((string)r["Grid Size"]).Split('x')[0]);
                Assert.That(C.GridSize(cc), Is.EqualTo(side), $"CC {cc}");
            }
        }

        [Test]
        public void CountLimitsComeFromBuildingCountByCc()
        {
            var gold = C.Get("gold_mine");
            Assert.That(Enumerable.Range(1, 10).Select(gold.MaxCountAt), Is.EqualTo(new[] { 1, 1, 2, 2, 2, 2, 2, 2, 2, 2 }));
            Assert.That(C.Get("cannon_tower").UnlockCc, Is.EqualTo(2));
            Assert.That(C.Get("dark_barracks").UnlockCc, Is.EqualTo(8));
            Assert.That(C.Get("patriot_sam_battery").UnlockCc, Is.EqualTo(8));
            Assert.That(C.CommandCenter.MaxCountAt(10), Is.EqualTo(1));
        }

        [Test]
        public void FootprintUnlockCcMatchesCountTable()
        {
            var rows = TestData.Data.Table("tables_07_BaseLayout.json").Rows("Building Footprints");
            foreach (var r in rows)
            {
                var name = (string)r["Building"];
                var def = C.Buildings.FirstOrDefault(b => b.Name == name);
                if (def == null) continue; // perimeter
                var cc = int.Parse(((string)r["Unlocked CC"]).Replace("CC", "").Trim());
                Assert.That(def.UnlockCc, Is.EqualTo(cc), name);
            }
        }

        [TestCase("command_center", 10)]
        [TestCase("gold_mine", 8)]
        [TestCase("gold_vault", 8)]
        [TestCase("barracks", 8)]
        [TestCase("air_hangar", 8)]
        [TestCase("dark_barracks", 5)]
        [TestCase("research_lab", 5)]
        [TestCase("army_camp", 8)]
        [TestCase("cannon_tower", 8)]
        [TestCase("ciws_tower", 6)]
        [TestCase("minefield_zone", 5)]
        [TestCase("war_bunker", 6)]
        [TestCase("wall_tile", 10)]
        public void MaxLevelsComeFromLevelTables(string id, int maxLevel)
        {
            Assert.That(C.Get(id).MaxLevel, Is.EqualTo(maxLevel));
        }

        [Test]
        public void PerLevelCcRequirements()
        {
            var lab = C.Get("research_lab");
            Assert.That(Enumerable.Range(1, 5).Select(lab.CcRequiredForLevel), Is.EqualTo(new[] { 1, 3, 5, 7, 9 }));
            var wall = C.Get("wall_tile");
            Assert.That(wall.CcRequiredForLevel(7), Is.EqualTo(7));
            Assert.That(C.Get("gold_mine").CcRequiredForLevel(8), Is.EqualTo(1), "no per-level rule in data");
        }

        [TestCase("88mm Flak Tower", "88mm_flak_tower")]
        [TestCase("S-400 SAM", "s_400_sam")]
        [TestCase("Command Center", "command_center")]
        public void MakeIdSlugs(string name, string id)
        {
            Assert.That(BuildingCatalog.MakeId(name), Is.EqualTo(id));
        }
    }
}
