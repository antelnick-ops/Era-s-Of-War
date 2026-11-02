using System;
using ErasOfWar.Simulation.Base;
using ErasOfWar.Simulation.Data;
using NUnit.Framework;

namespace ErasOfWar.Simulation.Tests
{
    /// <summary>Real game_data loaded once for all tests.</summary>
    internal static class TestData
    {
        private static readonly Lazy<GameData> _data = new Lazy<GameData>(() =>
        {
            var dir = GameDataLoader.FindGameDataDirectory(TestContext.CurrentContext.TestDirectory)
                      ?? throw new InvalidOperationException("game_data/ not found above the test folder");
            return GameDataLoader.LoadFromDirectory(dir);
        });

        private static readonly Lazy<BuildingCatalog> _catalog =
            new Lazy<BuildingCatalog>(() => BuildingCatalog.FromGameData(_data.Value));

        public static GameData Data => _data.Value;
        public static BuildingCatalog Catalog => _catalog.Value;

        public static BaseState NewBase() => new BaseState(Catalog);

        /// <summary>A base with a Command Center at the bottom-left, upgraded to the given level.</summary>
        public static BaseState BaseWithCc(int ccLevel)
        {
            var s = NewBase();
            var cc = s.Place("command_center", 0, 0);
            Assert.That(cc.Ok, cc.Message);
            for (var i = 1; i < ccLevel; i++)
                Assert.That(s.Upgrade(cc.BuildingId).Ok);
            return s;
        }
    }
}
