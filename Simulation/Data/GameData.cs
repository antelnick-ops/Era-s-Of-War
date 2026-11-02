using System;
using System.Collections.Generic;
using System.Linq;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>All static game data, loaded once from game_data/*.json. Treat as read-only.</summary>
    public sealed class GameData
    {
        public IReadOnlyList<UnitDef> Units { get; }
        public TechTreeDef TechTree { get; }
        public EconomyDef Economy { get; }
        public ConstantsDef Constants { get; }
        public IReadOnlyList<LeaderDef> Leaders { get; }
        public IReadOnlyList<GeneralDef> Generals { get; }

        public IReadOnlyDictionary<string, UnitDef> UnitsById { get; }

        /// <summary>Raw workbook exports by file name (e.g. "tables_04_Buildings.json").</summary>
        public IReadOnlyDictionary<string, TableFile> Tables { get; }

        public GameData(
            IReadOnlyList<UnitDef> units,
            TechTreeDef techTree,
            EconomyDef economy,
            ConstantsDef constants,
            IReadOnlyList<LeaderDef> leaders,
            IReadOnlyList<GeneralDef> generals,
            IReadOnlyDictionary<string, TableFile> tables = null)
        {
            Tables = tables ?? new Dictionary<string, TableFile>();
            Units = units ?? throw new ArgumentNullException(nameof(units));
            TechTree = techTree ?? throw new ArgumentNullException(nameof(techTree));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            Constants = constants ?? throw new ArgumentNullException(nameof(constants));
            Leaders = leaders ?? throw new ArgumentNullException(nameof(leaders));
            Generals = generals ?? throw new ArgumentNullException(nameof(generals));

            var byId = new Dictionary<string, UnitDef>(StringComparer.Ordinal);
            foreach (var u in units)
            {
                if (byId.ContainsKey(u.Id))
                    throw new GameDataException($"Duplicate unit id '{u.Id}' in units.json");
                byId.Add(u.Id, u);
            }
            UnitsById = byId;
        }

        public TableFile Table(string fileName) =>
            Tables.TryGetValue(fileName, out var t)
                ? t
                : throw new GameDataException($"{fileName} was not loaded");

        public bool TryGetUnit(string id, out UnitDef unit) =>
            UnitsById.TryGetValue(id ?? "", out unit);

        public IEnumerable<UnitDef> UnitsOf(string nation) =>
            Units.Where(u => u.Nation == nation);

        /// <summary>One-line summary for logs, e.g. "175 units (Germany 35, USSR 35, ...)".</summary>
        public string Summary()
        {
            var perNation = string.Join(", ",
                Constants.Nations.Select(n => $"{n} {Units.Count(u => u.Nation == n)}"));
            return $"{Units.Count} units ({perNation}), {TechTree.Units.Count} tech tree nodes, " +
                   $"{Leaders.Count} leaders, {Generals.Count} generals, {Economy.Packs.Count} War Bond packs";
        }
    }

    public sealed class GameDataException : Exception
    {
        public GameDataException(string message) : base(message) { }
        public GameDataException(string message, Exception inner) : base(message, inner) { }
    }
}
