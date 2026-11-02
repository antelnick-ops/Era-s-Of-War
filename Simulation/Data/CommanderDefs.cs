using System.Globalization;
using Newtonsoft.Json;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>One leader from game_data/leaders.json (pick one: passive + one active per raid).</summary>
    public sealed class LeaderDef
    {
        [JsonProperty("nation", Required = Required.Always)] public string Nation { get; set; }
        [JsonProperty("name", Required = Required.Always)] public string Name { get; set; }
        [JsonProperty("nick")] public string Nickname { get; set; }
        [JsonProperty("passive")] public string Passive { get; set; }
        [JsonProperty("active")] public string Active { get; set; }
        [JsonProperty("unlock")] public string Unlock { get; set; }
        [JsonProperty("tier")] public string TierRequirement { get; set; }
        [JsonProperty("note")] public string Note { get; set; }
    }

    /// <summary>One general from game_data/generals.json (a placeable unit with an aura).</summary>
    public sealed class GeneralDef
    {
        [JsonProperty("nation", Required = Required.Always)] public string Nation { get; set; }
        [JsonProperty("name", Required = Required.Always)] public string Name { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        /// <summary>First buffed unit class.</summary>
        [JsonProperty("c1")] public string Class1 { get; set; }
        /// <summary>Second buffed unit class.</summary>
        [JsonProperty("c2")] public string Class2 { get; set; }
        [JsonProperty("hp", Required = Required.Always)] public int Hp { get; set; }
        [JsonProperty("dps", Required = Required.Always)] public int Dps { get; set; }
        [JsonProperty("speed")] public string Speed { get; set; }
        [JsonProperty("range")] public string Range { get; set; }
        /// <summary>Display string such as "35%". See <see cref="ArmorPct"/>.</summary>
        [JsonProperty("armor")] public string Armor { get; set; }
        [JsonProperty("ability")] public string Ability { get; set; }
        [JsonProperty("aura")] public string Aura { get; set; }
        [JsonProperty("note")] public string Note { get; set; }

        /// <summary>Armor as a number ("35%" -> 35). 0 if missing or unparseable.</summary>
        [JsonIgnore]
        public double ArmorPct =>
            double.TryParse((Armor ?? "").Trim().TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
