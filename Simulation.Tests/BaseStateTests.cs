using ErasOfWar.Simulation.Base;
using NUnit.Framework;

namespace ErasOfWar.Simulation.Tests
{
    [TestFixture]
    public class BaseStateTests
    {
        // ---------------------------------------------------------------- bounds

        [Test]
        public void NewBaseIsCc1With20x20Grid()
        {
            var s = TestData.NewBase();
            Assert.That(s.CcLevel, Is.EqualTo(1));
            Assert.That(s.GridSize, Is.EqualTo(20));
            Assert.That(s.Buildings, Is.Empty);
        }

        [TestCase(0, 0, true)]
        [TestCase(16, 16, true)]   // 4x4 CC ends exactly on the edge
        [TestCase(17, 16, false)]
        [TestCase(16, 17, false)]
        [TestCase(-1, 0, false)]
        [TestCase(0, -1, false)]
        [TestCase(30, 30, false)]
        public void CommandCenterMustFitInsideGrid(int x, int y, bool ok)
        {
            var r = TestData.NewBase().CanPlace("command_center", x, y);
            Assert.That(r.Ok, Is.EqualTo(ok), r.Message);
            if (!ok) Assert.That(r.Error, Is.EqualTo(BaseError.OutOfBounds));
        }

        [Test]
        public void OneByOneFitsInLastTile()
        {
            Assert.That(TestData.NewBase().Place("wall_tile", 19, 19).Ok);
            Assert.That(TestData.NewBase().Place("wall_tile", 20, 19).Error, Is.EqualTo(BaseError.OutOfBounds));
        }

        // ---------------------------------------------------------------- overlap

        [Test]
        public void BuildingsCannotOverlap()
        {
            var s = TestData.BaseWithCc(1);               // CC covers (0..3, 0..3)
            var r = s.Place("gold_mine", 3, 3);           // shares tile (3,3)
            Assert.That(r.Error, Is.EqualTo(BaseError.Overlap));
            Assert.That(r.Message, Does.Contain("Command Center"));
        }

        [Test]
        public void TouchingEdgesIsNotOverlap()
        {
            var s = TestData.BaseWithCc(1);
            Assert.That(s.Place("gold_mine", 4, 0).Ok);   // right of CC
            Assert.That(s.Place("oil_derrick", 0, 4).Ok); // above CC
        }

        [Test]
        public void CanPlaceDoesNotChangeState()
        {
            var s = TestData.NewBase();
            var before = s.Revision;
            Assert.That(s.CanPlace("command_center", 5, 5).Ok);
            Assert.That(s.Buildings, Is.Empty);
            Assert.That(s.Revision, Is.EqualTo(before));
        }

        [Test]
        public void BuildingAtFindsEveryCoveredTile()
        {
            var s = TestData.NewBase();
            var id = s.Place("barracks", 5, 6).BuildingId;
            Assert.That(s.BuildingAt(5, 6)?.Id, Is.EqualTo(id));
            Assert.That(s.BuildingAt(7, 8)?.Id, Is.EqualTo(id));
            Assert.That(s.BuildingAt(8, 8), Is.Null);
            Assert.That(s.BuildingAt(-1, 0), Is.Null);
        }

        // ---------------------------------------------------------------- CC gating

        [Test]
        public void CannonTowerIsLockedUntilCc2()
        {
            var s = TestData.BaseWithCc(1);
            var r = s.Place("cannon_tower", 10, 10);
            Assert.That(r.Error, Is.EqualTo(BaseError.LockedByCc));
            Assert.That(r.RequiredCc, Is.EqualTo(2));

            Assert.That(s.Upgrade(s.CommandCenter.Id).Ok);
            Assert.That(s.Place("cannon_tower", 10, 10).Ok);
        }

        [Test]
        public void TierFiveBuildingsLockedAtLowCc()
        {
            var s = TestData.BaseWithCc(7);
            Assert.That(s.CanPlace("dark_barracks", 15, 15).RequiredCc, Is.EqualTo(8));
        }

        [Test]
        public void LevelGatedByCcWhereDataSaysSo()
        {
            var s = TestData.BaseWithCc(2);
            var lab = s.Place("research_lab", 10, 10).BuildingId;
            var r = s.Upgrade(lab);
            Assert.That(r.Error, Is.EqualTo(BaseError.LevelNeedsCc));
            Assert.That(r.RequiredCc, Is.EqualTo(3));

            Assert.That(s.Upgrade(s.CommandCenter.Id).Ok); // CC 3
            Assert.That(s.Upgrade(lab).Ok);
            Assert.That(s.Get(lab).Level, Is.EqualTo(2));
        }

        [Test]
        public void WallTierGatedByCc()
        {
            var s = TestData.BaseWithCc(1);
            var w = s.Place("wall_tile", 10, 10).BuildingId;
            Assert.That(s.Upgrade(w).Error, Is.EqualTo(BaseError.LevelNeedsCc));
        }

        [Test]
        public void UpgradeStopsAtMaxLevel()
        {
            var s = TestData.BaseWithCc(10);
            Assert.That(s.CcLevel, Is.EqualTo(10));
            var r = s.Upgrade(s.CommandCenter.Id);
            Assert.That(r.Error, Is.EqualTo(BaseError.MaxLevel));
        }

        // ---------------------------------------------------------------- count limits

        [Test]
        public void SecondGoldMineNeedsCc3()
        {
            var s = TestData.BaseWithCc(1);
            Assert.That(s.Place("gold_mine", 10, 10).Ok);
            var r = s.Place("gold_mine", 12, 10);
            Assert.That(r.Error, Is.EqualTo(BaseError.CountLimitReached));
            Assert.That(r.RequiredCc, Is.EqualTo(3), "hint says when more unlock");

            s.Upgrade(s.CommandCenter.Id); // CC 2: still 1
            Assert.That(s.CanPlace("gold_mine", 12, 10).Error, Is.EqualTo(BaseError.CountLimitReached));
            s.Upgrade(s.CommandCenter.Id); // CC 3: 2 allowed
            var free = FindFreeSpot(s, "gold_mine");
            Assert.That(s.Place("gold_mine", free.x, free.y).Ok);
            Assert.That(s.CountOf("gold_mine"), Is.EqualTo(2));
        }

        [Test]
        public void OnlyOneCommandCenter()
        {
            var s = TestData.BaseWithCc(1);
            Assert.That(s.Place("command_center", 10, 10).Error, Is.EqualTo(BaseError.CountLimitReached));
        }

        [Test]
        public void WallTileLimitIs50AtCc1()
        {
            var s = TestData.NewBase();
            var placed = 0;
            for (var y = 0; y < 20 && placed < 50; y++)
            for (var x = 0; x < 20 && placed < 50; x++)
                if (s.Place("wall_tile", x, y).Ok) placed++;

            Assert.That(placed, Is.EqualTo(50));
            var r = s.Place("wall_tile", 19, 19);
            Assert.That(r.Error, Is.EqualTo(BaseError.CountLimitReached));
            Assert.That(r.Message, Does.Contain("50/50"));
        }

        [Test]
        public void RemovingFreesTheSlot()
        {
            var s = TestData.BaseWithCc(1);
            var mine = s.Place("gold_mine", 10, 10).BuildingId;
            Assert.That(s.Remove(mine).Ok);
            Assert.That(s.Place("gold_mine", 12, 12).Ok);
        }

        // ---------------------------------------------------------------- move / remove

        [Test]
        public void MoveToFreeSpot()
        {
            var s = TestData.BaseWithCc(1);
            var mine = s.Place("gold_mine", 10, 10).BuildingId;
            Assert.That(s.Move(mine, 11, 10).Ok, "can overlap its own old tiles");
            Assert.That((s.Get(mine).X, s.Get(mine).Y), Is.EqualTo((11, 10)));
            Assert.That(s.BuildingAt(10, 10), Is.Null);
        }

        [Test]
        public void BlockedMoveLeavesBuildingWhereItWas()
        {
            var s = TestData.BaseWithCc(1);
            var mine = s.Place("gold_mine", 10, 10).BuildingId;
            Assert.That(s.Move(mine, 2, 2).Error, Is.EqualTo(BaseError.Overlap));
            Assert.That(s.Move(mine, 19, 19).Error, Is.EqualTo(BaseError.OutOfBounds));
            Assert.That((s.Get(mine).X, s.Get(mine).Y), Is.EqualTo((10, 10)));
        }

        [Test]
        public void CommandCenterCannotBeRemoved()
        {
            var s = TestData.BaseWithCc(1);
            Assert.That(s.Remove(s.CommandCenter.Id).Error, Is.EqualTo(BaseError.CannotRemoveCommandCenter));
        }

        [Test]
        public void UnknownIdsAndTypesFailCleanly()
        {
            var s = TestData.NewBase();
            Assert.That(s.Move(99, 0, 0).Error, Is.EqualTo(BaseError.NotFound));
            Assert.That(s.Remove(99).Error, Is.EqualTo(BaseError.NotFound));
            Assert.That(s.Upgrade(99).Error, Is.EqualTo(BaseError.NotFound));
            Assert.That(s.Place("death_star", 0, 0).Error, Is.EqualTo(BaseError.UnknownType));
        }

        // ---------------------------------------------------------------- grid growth

        [Test]
        public void CcUpgradeGrowsGridAndKeepsBaseCentred()
        {
            var s = TestData.NewBase();
            var cc = s.Place("command_center", 8, 8).BuildingId;
            var mine = s.Place("gold_mine", 0, 0).BuildingId;

            Assert.That(s.Upgrade(cc).Ok);
            Assert.That(s.GridSize, Is.EqualTo(22));
            Assert.That((s.Get(cc).X, s.Get(mine).X), Is.EqualTo((9, 1)), "shifted by (22-20)/2 = 1");

            // CC6 -> CC7 grows 30 -> 33: shift is 1 (rounded down), everything still inside
            for (var i = 0; i < 5; i++) s.Upgrade(cc);
            Assert.That(s.GridSize, Is.EqualTo(33));
            foreach (var b in s.Buildings)
                Assert.That(b.X + b.Width <= s.GridSize && b.Y + b.Height <= s.GridSize, b.ToString());
        }

        private static (int x, int y) FindFreeSpot(BaseState s, string type)
        {
            for (var y = 0; y < s.GridSize; y++)
            for (var x = 0; x < s.GridSize; x++)
                if (s.CanPlace(type, x, y).Ok) return (x, y);
            Assert.Fail("no free spot");
            return (0, 0);
        }
    }
}
