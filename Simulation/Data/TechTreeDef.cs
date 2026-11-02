using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>game_data/tech_tree.json.</summary>
    public sealed class TechTreeDef
    {
        /// <summary>
        /// Raw table as exported from WTBB_05: a single key (the sheet note) mapping to the tier rows.
        /// Use <see cref="TierUnlocks"/> instead.
        /// </summary>
        [JsonProperty("tier_unlock", Required = Required.Always)]
        public Dictionary<string, List<TierUnlockDef>> TierUnlockTable { get; set; }

        /// <summary>Total RP to research every vehicle in a tier, keyed "1".."5".</summary>
        [JsonProperty("tier_rp", Required = Required.Always)]
        public Dictionary<string, int> TierRp { get; set; }

        [JsonProperty("units", Required = Required.Always)]
        public List<TechTreeEntry> Units { get; set; }

        [JsonIgnore]
        public IReadOnlyList<TierUnlockDef> TierUnlocks =>
            TierUnlockTable.Values.FirstOrDefault() ?? new List<TierUnlockDef>();
    }

    /// <summary>One research node: a unit, its RP cost and its prerequisite.</summary>
    public sealed class TechTreeEntry
    {
        [JsonProperty("unit_id", Required = Required.Always)] public string UnitId { get; set; }
        [JsonProperty("nation", Required = Required.Always)] public string Nation { get; set; }
        [JsonProperty("tier", Required = Required.Always)] public int Tier { get; set; }
        [JsonProperty("rp_cost")] public int RpCost { get; set; }
        [JsonProperty("gold_to_train_first")] public int GoldToTrainFirst { get; set; }
        /// <summary>Null for a nation's root unit.</summary>
        [JsonProperty("prerequisite_id")] public string PrerequisiteId { get; set; }
        [JsonProperty("prerequisite_rule")] public string PrerequisiteRule { get; set; }
    }

    /// <summary>A tier gate row (CC, lab, trophies, cost, time). Text fields are display strings from the workbook.</summary>
    public sealed class TierUnlockDef
    {
        [JsonProperty("Tier")] public string Tier { get; set; }
        [JsonProperty("CC Required")] public string CcRequired { get; set; }
        [JsonProperty("Research Lab")] public string ResearchLab { get; set; }
        [JsonProperty("Trophies")] public string Trophies { get; set; }
        [JsonProperty("Gold Cost")] public int GoldCost { get; set; }
        [JsonProperty("Steel Cost")] public int SteelCost { get; set; }
        [JsonProperty("Unlock Time")] public string UnlockTime { get; set; }
        [JsonProperty("Nation")] public string Nation { get; set; }
        [JsonProperty("Note")] public string Note { get; set; }
    }
}
