using System;
using System.Collections.Generic;
using System.Linq;

namespace ErasOfWar.Simulation.Base
{
    /// <summary>A building on the grid. (X, Y) is its bottom-left tile; it covers Width x Height tiles.</summary>
    public sealed class PlacedBuilding
    {
        public int Id { get; }
        public BuildingDef Def { get; }
        public string TypeId => Def.Id;
        public int Level { get; internal set; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int Width => Def.Width;
        public int Height => Def.Height;

        internal PlacedBuilding(int id, BuildingDef def, int level, int x, int y)
        {
            Id = id;
            Def = def;
            Level = level;
            X = x;
            Y = y;
        }

        public bool Covers(int tx, int ty) => tx >= X && tx < X + Width && ty >= Y && ty < Y + Height;

        public override string ToString() => $"#{Id} {Def.Name} Lv{Level} at ({X},{Y})";
    }

    /// <summary>
    /// The player's base: grid size by CC level and the placed buildings. All placement rules live here
    /// (bounds, overlap, CC gating, count limits, wall tile limit, max level). No Unity types.
    /// Perimeter wall is outside the grid and not part of the state.
    /// </summary>
    public sealed class BaseState
    {
        private readonly List<PlacedBuilding> _buildings = new List<PlacedBuilding>();
        private int[] _occupancy = Array.Empty<int>(); // GridSize^2, 0 = empty, else building id
        private int _occupancySize;

        public BuildingCatalog Catalog { get; }
        public IReadOnlyList<PlacedBuilding> Buildings => _buildings;
        /// <summary>Next id handed to a placed building.</summary>
        public int NextId { get; private set; } = 1;
        /// <summary>Increments on every change. Views can redraw when it differs from what they drew.</summary>
        public int Revision { get; private set; }

        public BaseState(BuildingCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            RebuildOccupancy();
        }

        // ------------------------------------------------------------------ queries

        public PlacedBuilding CommandCenter => _buildings.FirstOrDefault(b => b.Def.IsCommandCenter);

        /// <summary>Level of the placed Command Center, or 1 for a new base without one.</summary>
        public int CcLevel => CommandCenter?.Level ?? 1;

        public int GridSize => Catalog.GridSize(CcLevel);

        public int CountOf(string typeId) => _buildings.Count(b => b.TypeId == typeId);

        public int MaxCountOf(string typeId) =>
            Catalog.TryGet(typeId, out var def) ? def.MaxCountAt(CcLevel) : 0;

        public PlacedBuilding Get(int id) => _buildings.FirstOrDefault(b => b.Id == id);

        /// <summary>The building covering a tile, or null.</summary>
        public PlacedBuilding BuildingAt(int x, int y)
        {
            if (!InGrid(x, y)) return null;
            var id = _occupancy[y * _occupancySize + x];
            return id == 0 ? null : Get(id);
        }

        public bool InGrid(int x, int y) => x >= 0 && y >= 0 && x < GridSize && y < GridSize;

        // ------------------------------------------------------------------ place

        public BaseResult CanPlace(string typeId, int x, int y)
        {
            if (!Catalog.TryGet(typeId, out var def))
                return BaseResult.Fail(BaseError.UnknownType, $"Unknown building type '{typeId}'.");

            var cc = CcLevel;
            var max = def.MaxCountAt(cc);
            if (max <= 0)
            {
                var unlock = def.UnlockCc;
                return BaseResult.Fail(BaseError.LockedByCc,
                    unlock > 0 ? $"{def.Name} unlocks at Command Center level {unlock}." : $"{def.Name} cannot be built.",
                    requiredCc: unlock);
            }

            var count = CountOf(typeId);
            if (count >= max)
            {
                var next = NextCcWithMore(def, max);
                var hint = next > 0 ? $" More at Command Center level {next}." : "";
                return BaseResult.Fail(BaseError.CountLimitReached,
                    $"{def.Name} limit reached ({count}/{max} at Command Center level {cc}).{hint}",
                    requiredCc: next);
            }

            return CheckFootprint(def, x, y, ignoreId: 0);
        }

        public BaseResult Place(string typeId, int x, int y)
        {
            var check = CanPlace(typeId, x, y);
            if (!check.Ok) return check;

            var def = Catalog.Get(typeId);
            var b = new PlacedBuilding(NextId++, def, 1, x, y);
            _buildings.Add(b);
            Changed();
            return BaseResult.Success(b.Id, $"Placed {def.Name}.");
        }

        // ------------------------------------------------------------------ move

        public BaseResult CanMove(int id, int x, int y)
        {
            var b = Get(id);
            if (b == null) return BaseResult.Fail(BaseError.NotFound, "That building no longer exists.");
            return CheckFootprint(b.Def, x, y, ignoreId: id);
        }

        public BaseResult Move(int id, int x, int y)
        {
            var check = CanMove(id, x, y);
            if (!check.Ok) return check;

            var b = Get(id);
            b.X = x;
            b.Y = y;
            Changed();
            return BaseResult.Success(id, $"Moved {b.Def.Name}.");
        }

        // ------------------------------------------------------------------ remove

        public BaseResult Remove(int id)
        {
            var b = Get(id);
            if (b == null) return BaseResult.Fail(BaseError.NotFound, "That building no longer exists.");
            if (b.Def.IsCommandCenter)
                return BaseResult.Fail(BaseError.CannotRemoveCommandCenter,
                    "The Command Center can be moved but not removed.", id);

            _buildings.Remove(b);
            Changed();
            return BaseResult.Success(id, $"Removed {b.Def.Name}.");
        }

        // ------------------------------------------------------------------ upgrade

        /// <summary>Level rules only. Costs and build timers arrive with the economy (Prompt 03).</summary>
        public BaseResult CanUpgrade(int id)
        {
            var b = Get(id);
            if (b == null) return BaseResult.Fail(BaseError.NotFound, "That building no longer exists.");

            if (b.Level >= b.Def.MaxLevel)
                return BaseResult.Fail(BaseError.MaxLevel, $"{b.Def.Name} is already at max level ({b.Def.MaxLevel}).", id);

            var next = b.Level + 1;
            var needCc = b.Def.CcRequiredForLevel(next);
            if (!b.Def.IsCommandCenter && needCc > CcLevel)
                return BaseResult.Fail(BaseError.LevelNeedsCc,
                    $"{b.Def.Name} level {next} needs Command Center level {needCc}.", id, needCc);

            return BaseResult.Success(id);
        }

        public BaseResult Upgrade(int id)
        {
            var check = CanUpgrade(id);
            if (!check.Ok) return check;

            var b = Get(id);
            var oldSize = GridSize;
            b.Level++;

            // A bigger Command Center grows the grid. Shift everything so the base stays centred.
            var newSize = GridSize;
            if (b.Def.IsCommandCenter && newSize > oldSize)
            {
                var shift = (newSize - oldSize) / 2;
                foreach (var other in _buildings)
                {
                    other.X += shift;
                    other.Y += shift;
                }
            }

            Changed();
            return BaseResult.Success(id, $"{b.Def.Name} upgraded to level {b.Level}.");
        }

        // ------------------------------------------------------------------ load support

        /// <summary>Re-adds a saved building with its id and level, using the same rules as Place.</summary>
        internal BaseResult Restore(int id, string typeId, int level, int x, int y)
        {
            if (id <= 0 || Get(id) != null)
                return BaseResult.Fail(BaseError.NotFound, $"Duplicate or invalid id {id}.");

            if (!Catalog.TryGet(typeId, out var def))
                return BaseResult.Fail(BaseError.UnknownType, $"Unknown building type '{typeId}'.");
            level = Math.Max(1, Math.Min(level, def.MaxLevel));

            if (def.IsCommandCenter && CommandCenter == null)
            {
                // The CC's own level sets the grid size, so check bounds against that grid.
                var size = Catalog.GridSize(level);
                if (x < 0 || y < 0 || x + def.Width > size || y + def.Height > size || _buildings.Count > 0)
                    return BaseResult.Fail(BaseError.OutOfBounds, $"{def.Name} must fit inside the {size}x{size} grid.");
            }
            else
            {
                var check = CanPlace(typeId, x, y);
                if (!check.Ok) return check;
            }

            var b = new PlacedBuilding(id, def, level, x, y);
            _buildings.Add(b);
            NextId = Math.Max(NextId, id + 1);
            Changed();
            return BaseResult.Success(id);
        }

        internal void SetNextId(int nextId) => NextId = Math.Max(NextId, nextId);

        // ------------------------------------------------------------------ internals

        private BaseResult CheckFootprint(BuildingDef def, int x, int y, int ignoreId)
        {
            var size = GridSize;
            if (x < 0 || y < 0 || x + def.Width > size || y + def.Height > size)
                return BaseResult.Fail(BaseError.OutOfBounds, $"{def.Name} must fit inside the {size}x{size} grid.");

            for (var ty = y; ty < y + def.Height; ty++)
            for (var tx = x; tx < x + def.Width; tx++)
            {
                var occupant = _occupancy[ty * _occupancySize + tx];
                if (occupant != 0 && occupant != ignoreId)
                    return BaseResult.Fail(BaseError.Overlap, $"{def.Name} overlaps {Get(occupant)?.Def.Name ?? "another building"}.");
            }

            return BaseResult.Success(ignoreId);
        }

        private int NextCcWithMore(BuildingDef def, int current)
        {
            for (var cc = CcLevel + 1; cc <= Catalog.MaxCcLevel; cc++)
                if (def.MaxCountAt(cc) > current) return cc;
            return 0;
        }

        private void Changed()
        {
            Revision++;
            RebuildOccupancy();
        }

        private void RebuildOccupancy()
        {
            var size = GridSize;
            if (_occupancySize != size || _occupancy.Length != size * size)
            {
                _occupancySize = size;
                _occupancy = new int[size * size];
            }
            else
            {
                Array.Clear(_occupancy, 0, _occupancy.Length);
            }

            foreach (var b in _buildings)
                for (var ty = b.Y; ty < b.Y + b.Height; ty++)
                for (var tx = b.X; tx < b.X + b.Width; tx++)
                    if (tx >= 0 && ty >= 0 && tx < size && ty < size)
                        _occupancy[ty * size + tx] = b.Id;
        }
    }
}
