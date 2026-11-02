using Newtonsoft.Json;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>One vehicle from game_data/units.json.</summary>
    public sealed class UnitDef
    {
        public const string DomainGround = "ground";
        public const string DomainAir = "air";

        [JsonProperty("id", Required = Required.Always)] public string Id { get; set; }
        [JsonProperty("nation", Required = Required.Always)] public string Nation { get; set; }
        [JsonProperty("tier", Required = Required.Always)] public int Tier { get; set; }
        [JsonProperty("name", Required = Required.Always)] public string Name { get; set; }
        /// <summary>Vehicle class, e.g. "Light Tank", "Jet Fighter".</summary>
        [JsonProperty("cls", Required = Required.Always)] public string Class { get; set; }
        [JsonProperty("era")] public string Era { get; set; }
        [JsonProperty("domain", Required = Required.Always)] public string Domain { get; set; }

        [JsonProperty("hp", Required = Required.Always)] public int Hp { get; set; }
        [JsonProperty("dps", Required = Required.Always)] public int Dps { get; set; }
        [JsonProperty("speed")] public string Speed { get; set; }
        [JsonProperty("range")] public string Range { get; set; }
        [JsonProperty("armor_pct")] public double ArmorPct { get; set; }

        [JsonProperty("gold", Required = Required.Always)] public int GoldCost { get; set; }
        [JsonProperty("train_s")] public int TrainSeconds { get; set; }
        [JsonProperty("housing", Required = Required.Always)] public int Housing { get; set; }
        /// <summary>Oil spent per deployment in a raid.</summary>
        [JsonProperty("oil", Required = Required.Always)] public int OilPerDeploy { get; set; }

        [JsonProperty("priority")] public string TargetPriority { get; set; }
        [JsonProperty("special")] public string Special { get; set; }

        [JsonIgnore] public bool IsAir => Domain == DomainAir;
        [JsonIgnore] public bool IsGround => Domain == DomainGround;

        public override string ToString() => $"{Id} ({Nation} T{Tier} {Class})";
    }
}
