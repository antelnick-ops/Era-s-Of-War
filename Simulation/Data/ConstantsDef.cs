using System.Collections.Generic;
using Newtonsoft.Json;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>game_data/constants.json: global rules and lookup tables.</summary>
    public sealed class ConstantsDef
    {
        [JsonProperty("nations", Required = Required.Always)] public List<string> Nations { get; set; }
        /// <summary>Nation -> bonus name (e.g. "hp_pct") -> percent.</summary>
        [JsonProperty("nation_bonuses")] public Dictionary<string, Dictionary<string, double>> NationBonuses { get; set; }
        [JsonProperty("tiers")] public int Tiers { get; set; }
        [JsonProperty("units_per_tier_per_nation")] public int UnitsPerTierPerNation { get; set; }
        [JsonProperty("steal_rates")] public ResourceRates StealRates { get; set; }
        [JsonProperty("raid_time_seconds")] public int RaidTimeSeconds { get; set; }
        [JsonProperty("deployment_zones")] public int DeploymentZones { get; set; }
        /// <summary>Zone ("north"...) -> formation slots.</summary>
        [JsonProperty("formation_slots")] public Dictionary<string, int> FormationSlots { get; set; }
        [JsonProperty("air_trigger_ground_losses_pct")] public int AirTriggerGroundLossesPct { get; set; }
        [JsonProperty("max_generals_per_raid")] public int MaxGeneralsPerRaid { get; set; }
        [JsonProperty("wounded_raids")] public int WoundedRaids { get; set; }
        [JsonProperty("cc_levels")] public int CcLevels { get; set; }
        /// <summary>"cc1".."cc10" -> grid side length in tiles.</summary>
        [JsonProperty("grid_by_cc")] public Dictionary<string, int> GridByCc { get; set; }
        /// <summary>Roman tier ("I".."V") -> Command Center level required.</summary>
        [JsonProperty("tier_requires_cc")] public Dictionary<string, int> TierRequiresCc { get; set; }
        [JsonProperty("shield_hours")] public int ShieldHours { get; set; }
        [JsonProperty("new_player_shield_days")] public int NewPlayerShieldDays { get; set; }
        /// <summary>Unit class -> housing space.</summary>
        [JsonProperty("housing")] public Dictionary<string, int> Housing { get; set; }
        /// <summary>Unit class -> Oil-per-deploy factor (Oil = round(Gold x factor)).</summary>
        [JsonProperty("oil_factor")] public Dictionary<string, double> OilFactor { get; set; }
    }
}
