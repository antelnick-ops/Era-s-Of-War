using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ErasOfWar.Simulation.Data;
using Newtonsoft.Json.Linq;

namespace ErasOfWar.Simulation.Base
{
    /// <summary>
    /// Every placeable building type plus grid / wall limits per CC level.
    /// Sources: WTBB_07 Building Footprints (types, sizes), WTBB_04 Building Count by CC (limits, the
    /// source of truth), WTBB_07 Grid Size by CC Level (wall tiles), constants.json (grid size),
    /// WTBB_03 / WTBB_04 level tables (max levels and per-level CC requirements).
    /// </summary>
    public sealed class BuildingCatalog
    {
        // Sheet names in the workbook exports.
        private const string FootprintSheet = "Building Footprints";
        private const string GridSheet = "Grid Size by CC Level";
        private const string CountSheet = "Building Count by CC";
        private const string CommandCenterSheet = "Command Center";
        private const string DefenseSheet = "Defense Buildings";
        private const string WallsSheet = "Walls";
        private const string TrainingSheet = "Training Buildings";

        // Footprint rows that are not player-placeable buildings.
        private const string PerimeterWallName = "Perimeter Wall Section";

        // WTBB_04 "Training Buildings" tables are titled by notes, not building names, so they are
        // matched by order. If the workbook titles them by name, the name match below picks them up instead.
        private static readonly string[] TrainingTableOrder = { "Barracks", "Air Hangar", "Dark Barracks" };

        private readonly Dictionary<string, BuildingDef> _byId;
        private readonly int[] _gridSize;   // index = CC - 1
        private readonly int[] _wallTiles;  // index = CC - 1

        /// <summary>Placeable buildings in workbook order.</summary>
        public IReadOnlyList<BuildingDef> Buildings { get; }
        public int MaxCcLevel { get; }
        public BuildingDef CommandCenter { get; }
        public BuildingDef WallTile { get; }
        /// <summary>Data problems that were worked around (empty when the data is clean).</summary>
        public IReadOnlyList<string> Warnings { get; }

        private BuildingCatalog(List<BuildingDef> defs, int[] gridSize, int[] wallTiles, List<string> warnings)
        {
            Buildings = defs;
            _byId = defs.ToDictionary(d => d.Id, StringComparer.Ordinal);
            _gridSize = gridSize;
            _wallTiles = wallTiles;
            MaxCcLevel = gridSize.Length;
            CommandCenter = defs.FirstOrDefault(d => d.IsCommandCenter)
                            ?? throw new GameDataException("No Command Center in Building Footprints");
            WallTile = defs.FirstOrDefault(d => d.IsWall);
            Warnings = warnings;
        }

        public bool TryGet(string id, out BuildingDef def) => _byId.TryGetValue(id ?? "", out def);

        public BuildingDef Get(string id) =>
            TryGet(id, out var d) ? d : throw new KeyNotFoundException($"Unknown building type '{id}'");

        /// <summary>Grid side length in tiles at a CC level (constants.json grid_by_cc).</summary>
        public int GridSize(int ccLevel) => _gridSize[ClampCc(ccLevel) - 1];

        /// <summary>Placeable wall tiles allowed at a CC level (perimeter not counted).</summary>
        public int WallTileLimit(int ccLevel) => _wallTiles[ClampCc(ccLevel) - 1];

        private int ClampCc(int cc) => Math.Max(1, Math.Min(cc, MaxCcLevel));

        // ------------------------------------------------------------------ building from data

        public static BuildingCatalog FromGameData(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var warnings = new List<string>();
            var buildings = data.Table(GameDataLoader.BuildingTablesFile);
            var layout = data.Table(GameDataLoader.BaseLayoutTablesFile);
            var resources = data.Table(GameDataLoader.ResourceTablesFile);

            var ccLevels = data.Constants.CcLevels;
            if (ccLevels < 1) throw new GameDataException("constants.json cc_levels must be >= 1");

            // Grid size and wall tiles per CC
            var gridSize = new int[ccLevels];
            for (var cc = 1; cc <= ccLevels; cc++)
            {
                if (!data.Constants.GridByCc.TryGetValue($"cc{cc}", out gridSize[cc - 1]))
                    throw new GameDataException($"constants.json grid_by_cc is missing cc{cc}");
            }

            var wallTiles = new int[ccLevels];
            foreach (var row in layout.Rows(GridSheet))
            {
                var cc = ParseCc(Str(row, "CC Level"));
                if (cc >= 1 && cc <= ccLevels) wallTiles[cc - 1] = Int(row, "Wall Tiles Available");
            }
            if (wallTiles.Any(w => w <= 0))
                throw new GameDataException($"{layout.FileName}: '{GridSheet}' needs Wall Tiles Available for CC 1-{ccLevels}");

            // Count limits by CC
            var countRows = buildings.Rows(CountSheet)
                .ToDictionary(r => Str(r, "Building"), r => r, StringComparer.OrdinalIgnoreCase);

            var defs = new List<BuildingDef>();
            var trainingTables = buildings.HasSheet(TrainingSheet) ? buildings.Tables(TrainingSheet) : null;

            foreach (var row in layout.Rows(FootprintSheet))
            {
                var name = Str(row, "Building");
                if (string.IsNullOrWhiteSpace(name) || name == PerimeterWallName) continue;

                var category = Str(row, "Category");
                var (w, h) = ParseFootprint(Str(row, "Footprint"), name);

                // ---- count limits
                int[] maxCount;
                if (category == BuildingDef.CategoryCommand)
                {
                    maxCount = Enumerable.Repeat(1, ccLevels).ToArray();
                }
                else if (category == BuildingDef.CategoryWall)
                {
                    maxCount = (int[])wallTiles.Clone();
                }
                else
                {
                    var countRow = FindCountRow(countRows, name, warnings);
                    maxCount = new int[ccLevels];
                    if (countRow == null)
                        warnings.Add($"{name}: not in '{CountSheet}', so it cannot be built");
                    else
                        for (var cc = 1; cc <= ccLevels; cc++)
                            maxCount[cc - 1] = ParseCount(countRow[$"CC {cc}"]);
                }

                // ---- levels
                int maxLevel;
                int[] levelCc = null;
                if (category == BuildingDef.CategoryCommand)
                {
                    maxLevel = buildings.Rows(CommandCenterSheet).Count;
                }
                else if (category == BuildingDef.CategoryWall)
                {
                    var tiers = buildings.Rows(WallsSheet)
                        .Where(r => Str(r, "Wall Tier").StartsWith("Tier", StringComparison.Ordinal)).ToList();
                    maxLevel = tiers.Count;
                    levelCc = tiers.Select(r => ParseCc(Str(r, "CC Required"))).ToArray();
                }
                else if (category == "Defense")
                {
                    var d = buildings.Rows(DefenseSheet)
                        .FirstOrDefault(r => string.Equals(Str(r, "Building Name"), name, StringComparison.OrdinalIgnoreCase));
                    maxLevel = d == null ? 0 : ParseLevel(Str(d, "Max Level"));
                }
                else
                {
                    var table = FindLevelTable(name, resources, buildings, trainingTables);
                    maxLevel = table?.Count ?? 0;
                    if (table != null && table.Count > 0 && table[0]["CC Required"] != null)
                        levelCc = table.Select(r => Math.Max(1, ParseCc(Str(r, "CC Required")))).ToArray();
                }

                if (maxLevel < 1)
                {
                    warnings.Add($"{name}: no level table found, max level set to 1");
                    maxLevel = 1;
                }

                defs.Add(new BuildingDef(MakeId(name), name, category, w, h, maxLevel, maxCount, levelCc));
            }

            var dupes = defs.GroupBy(d => d.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupes.Count > 0)
                throw new GameDataException("Duplicate building ids: " + string.Join(", ", dupes));

            return new BuildingCatalog(defs, gridSize, wallTiles, warnings);
        }

        private static JObject FindCountRow(Dictionary<string, JObject> rows, string name, List<string> warnings)
        {
            if (rows.TryGetValue(name, out var exact)) return exact;

            // Names differ slightly between workbooks (e.g. "Patriot SAM Battery" vs "Patriot SAM").
            var prefix = rows.Where(kv => name.StartsWith(kv.Key + " ", StringComparison.OrdinalIgnoreCase)
                                          || kv.Key.StartsWith(name + " ", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (prefix.Count == 1)
            {
                warnings.Add($"Name mismatch: '{name}' (Footprints) matched to '{prefix[0].Key}' ({CountSheet})");
                return prefix[0].Value;
            }
            return null;
        }

        private static IReadOnlyList<JObject> FindLevelTable(string name, TableFile resources, TableFile buildings,
            IReadOnlyList<KeyValuePair<string, IReadOnlyList<JObject>>> trainingTables)
        {
            var candidates = new List<KeyValuePair<string, IReadOnlyList<JObject>>>();
            foreach (var sheet in new[] { "Passive Generation", "Storage Buildings" })
                if (resources.HasSheet(sheet)) candidates.AddRange(resources.Tables(sheet));
            foreach (var sheet in new[] { "Support Buildings", "Alliance Buildings", TrainingSheet })
                if (buildings.HasSheet(sheet)) candidates.AddRange(buildings.Tables(sheet));

            // Title starts with the building name in capitals, e.g. "GOLD VAULT - Steal Rate: 20%"
            var upper = name.ToUpperInvariant();
            foreach (var t in candidates)
            {
                var title = t.Key.ToUpperInvariant();
                if (title == upper || title.StartsWith(upper + " ", StringComparison.Ordinal))
                    return t.Value;
            }

            var i = Array.IndexOf(TrainingTableOrder, name);
            if (i >= 0 && trainingTables != null && i < trainingTables.Count)
                return trainingTables[i].Value;

            return null;
        }

        // ------------------------------------------------------------------ parsing helpers

        private static string Str(JObject row, string column) => (string)row[column] ?? "";

        private static int Int(JObject row, string column)
        {
            var t = row[column];
            if (t == null) return 0;
            if (t.Type == JTokenType.Integer || t.Type == JTokenType.Float) return (int)t;
            return int.TryParse(((string)t ?? "").Replace(",", ""), out var v) ? v : 0;
        }

        /// <summary>"CC 3", "CC3", "CC9 + Dark Barracks" -> 3 / 3 / 9. 0 if none.</summary>
        internal static int ParseCc(string text)
        {
            var m = Regex.Match(text ?? "", @"CC\s*(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        /// <summary>"Lv 8" -> 8. 0 if none.</summary>
        internal static int ParseLevel(string text)
        {
            var m = Regex.Match(text ?? "", @"(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        /// <summary>Count cell: "2" -> 2, "-" or empty -> 0.</summary>
        internal static int ParseCount(JToken cell)
        {
            if (cell == null) return 0;
            if (cell.Type == JTokenType.Integer) return (int)cell;
            return int.TryParse(((string)cell ?? "").Trim(), out var v) ? v : 0;
        }

        private static (int, int) ParseFootprint(string text, string name)
        {
            var m = Regex.Match(text ?? "", @"^\s*(\d+)\s*x\s*(\d+)\s*$", RegexOptions.IgnoreCase);
            if (!m.Success) throw new GameDataException($"{name}: bad footprint '{text}' (expected like '3x3')");
            return (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
        }

        /// <summary>"88mm Flak Tower" -> "88mm_flak_tower", "S-400 SAM" -> "s_400_sam".</summary>
        public static string MakeId(string name)
        {
            var sb = new StringBuilder();
            foreach (var ch in name.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }
    }
}
