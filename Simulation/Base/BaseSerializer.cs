using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ErasOfWar.Simulation.Base
{
    /// <summary>Saves a base to JSON and loads it back. Unity decides where the file lives.</summary>
    public static class BaseSerializer
    {
        public const int CurrentVersion = 1;

        public static string ToJson(BaseState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var save = new BaseSave
            {
                Version = CurrentVersion,
                NextId = state.NextId,
                Buildings = state.Buildings
                    .Select(b => new BaseSave.Building { Id = b.Id, Type = b.TypeId, Level = b.Level, X = b.X, Y = b.Y })
                    .ToList(),
            };
            return JsonConvert.SerializeObject(save, Formatting.Indented);
        }

        /// <summary>
        /// Rebuilds a base from JSON. Entries that break the current rules (unknown type, overlap,
        /// off-grid, over the limit) are dropped and listed in <see cref="BaseLoadResult.Dropped"/>.
        /// Unreadable JSON gives an empty base and an <see cref="BaseLoadResult.Error"/>.
        /// </summary>
        public static BaseLoadResult FromJson(BuildingCatalog catalog, string json)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var state = new BaseState(catalog);

            BaseSave save;
            try
            {
                save = JsonConvert.DeserializeObject<BaseSave>(json ?? "");
            }
            catch (JsonException e)
            {
                return new BaseLoadResult(state, new List<string>(), "Save file is unreadable: " + e.Message);
            }
            if (save == null)
                return new BaseLoadResult(state, new List<string>(), "Save file is empty.");
            if (save.Version > CurrentVersion)
                return new BaseLoadResult(state, new List<string>(),
                    $"Save file version {save.Version} is newer than this game ({CurrentVersion}).");

            var dropped = new List<string>();
            var entries = save.Buildings ?? new List<BaseSave.Building>();

            // Command Center first: its level sets the grid size and limits for everything else.
            var ordered = entries
                .OrderByDescending(e => e != null && catalog.TryGet(e.Type, out var d) && d.IsCommandCenter)
                .ToList();

            foreach (var e in ordered)
            {
                if (e == null) continue;
                var result = state.Restore(e.Id, e.Type, e.Level, e.X, e.Y);
                if (!result.Ok) dropped.Add($"{e.Type ?? "?"} #{e.Id}: {result.Message}");
            }

            state.SetNextId(save.NextId);
            return new BaseLoadResult(state, dropped, null);
        }

        /// <summary>On-disk shape. Field names are part of the save format: don't rename.</summary>
        private sealed class BaseSave
        {
            [JsonProperty("version")] public int Version { get; set; }
            [JsonProperty("next_id")] public int NextId { get; set; }
            [JsonProperty("buildings")] public List<Building> Buildings { get; set; }

            public sealed class Building
            {
                [JsonProperty("id")] public int Id { get; set; }
                [JsonProperty("type")] public string Type { get; set; }
                [JsonProperty("level")] public int Level { get; set; }
                [JsonProperty("x")] public int X { get; set; }
                [JsonProperty("y")] public int Y { get; set; }
            }
        }
    }

    public sealed class BaseLoadResult
    {
        public BaseState State { get; }
        /// <summary>Saved buildings that could not be restored, with the reason.</summary>
        public IReadOnlyList<string> Dropped { get; }
        /// <summary>Null when the file was read; otherwise why it could not be (State is then an empty base).</summary>
        public string Error { get; }
        public bool Ok => Error == null;

        internal BaseLoadResult(BaseState state, IReadOnlyList<string> dropped, string error)
        {
            State = state;
            Dropped = dropped;
            Error = error;
        }
    }
}
