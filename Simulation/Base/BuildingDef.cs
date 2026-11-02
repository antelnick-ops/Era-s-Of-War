using System;

namespace ErasOfWar.Simulation.Base
{
    /// <summary>Static rules for one placeable building type, built from the WTBB_04 / WTBB_07 tables.</summary>
    public sealed class BuildingDef
    {
        public const string CategoryCommand = "Command";
        public const string CategoryWall = "Wall";

        private readonly int[] _maxCountByCc;   // index = CC level - 1
        private readonly int[] _ccRequiredByLevel; // index = level - 1; null = no per-level CC rule

        /// <summary>Stable id used in saves, e.g. "gold_mine".</summary>
        public string Id { get; }
        public string Name { get; }
        /// <summary>Command, Training, Resource, Defense, Support, Alliance or Wall.</summary>
        public string Category { get; }
        public int Width { get; }
        public int Height { get; }
        public int MaxLevel { get; }

        public bool IsCommandCenter => Category == CategoryCommand;
        public bool IsWall => Category == CategoryWall;

        public BuildingDef(string id, string name, string category, int width, int height,
            int maxLevel, int[] maxCountByCc, int[] ccRequiredByLevel)
        {
            if (width < 1 || height < 1) throw new ArgumentException($"{name}: footprint must be at least 1x1");
            if (maxLevel < 1) throw new ArgumentException($"{name}: max level must be at least 1");
            Id = id;
            Name = name;
            Category = category;
            Width = width;
            Height = height;
            MaxLevel = maxLevel;
            _maxCountByCc = maxCountByCc ?? throw new ArgumentNullException(nameof(maxCountByCc));
            _ccRequiredByLevel = ccRequiredByLevel;
        }

        /// <summary>How many of this building a base may have at a CC level (0 = not unlocked yet).</summary>
        public int MaxCountAt(int ccLevel)
        {
            if (_maxCountByCc.Length == 0) return 0;
            var i = Math.Max(1, Math.Min(ccLevel, _maxCountByCc.Length)) - 1;
            return _maxCountByCc[i];
        }

        /// <summary>First CC level where at least one can be built, or 0 if never.</summary>
        public int UnlockCc
        {
            get
            {
                for (var i = 0; i < _maxCountByCc.Length; i++)
                    if (_maxCountByCc[i] > 0) return i + 1;
                return 0;
            }
        }

        /// <summary>CC level needed to reach <paramref name="level"/> (1 when the data has no per-level rule).</summary>
        public int CcRequiredForLevel(int level)
        {
            if (_ccRequiredByLevel == null || level < 1 || level > _ccRequiredByLevel.Length) return 1;
            return _ccRequiredByLevel[level - 1];
        }

        public override string ToString() => $"{Name} ({Width}x{Height})";
    }
}
