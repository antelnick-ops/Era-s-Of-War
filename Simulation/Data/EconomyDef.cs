using System.Collections.Generic;
using Newtonsoft.Json;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>game_data/economy.json: War Bonds, speed-ups, crews, shields, loot rules.</summary>
    public sealed class EconomyDef
    {
        [JsonProperty("premium_currency")] public string PremiumCurrency { get; set; }
        [JsonProperty("packs", Required = Required.Always)] public List<WarBondPackDef> Packs { get; set; }
        [JsonProperty("speedup_anchors")] public List<SpeedupPoint> SpeedupAnchors { get; set; }
        [JsonProperty("speedup_table")] public List<SpeedupPoint> SpeedupTable { get; set; }
        [JsonProperty("crews")] public List<EngineerCrewDef> Crews { get; set; }
        [JsonProperty("shields")] public List<ShieldDef> Shields { get; set; }
        [JsonProperty("resource_packs")] public List<ResourcePackDef> ResourcePacks { get; set; }
        [JsonProperty("resource_pack_rules")] public ResourcePackRules ResourcePackRules { get; set; }
        [JsonProperty("oil_divisor")] public double OilDivisor { get; set; }
        [JsonProperty("steel_divisor")] public double SteelDivisor { get; set; }
        [JsonProperty("cosmetics")] public List<CosmeticDef> Cosmetics { get; set; }
        [JsonProperty("free_wb")] public List<FreeWarBondSource> FreeWb { get; set; }
        [JsonProperty("loot_caps")] public List<LootCapDef> LootCaps { get; set; }
        [JsonProperty("protected_storage_pct")] public double ProtectedStoragePct { get; set; }
        [JsonProperty("steal_rates", Required = Required.Always)] public ResourceRates StealRates { get; set; }
        [JsonProperty("offers")] public List<OfferDef> Offers { get; set; }
        [JsonProperty("store_fee")] public double StoreFee { get; set; }
        [JsonProperty("never_sold")] public List<string> NeverSold { get; set; }
    }

    public sealed class WarBondPackDef
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("price_usd")] public double PriceUsd { get; set; }
        [JsonProperty("base_wb")] public int BaseWb { get; set; }
        [JsonProperty("bonus_pct")] public double BonusPct { get; set; }
        [JsonProperty("total_wb")] public int TotalWb { get; set; }
        [JsonProperty("first_purchase_wb")] public int FirstPurchaseWb { get; set; }
    }

    public sealed class SpeedupPoint
    {
        [JsonProperty("minutes")] public int Minutes { get; set; }
        [JsonProperty("wb")] public int Wb { get; set; }
    }

    public sealed class EngineerCrewDef
    {
        [JsonProperty("crew")] public int Crew { get; set; }
        [JsonProperty("needs_cc")] public int NeedsCc { get; set; }
        [JsonProperty("wb")] public int Wb { get; set; }
    }

    public sealed class ShieldDef
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("days")] public int Days { get; set; }
        [JsonProperty("wb")] public int Wb { get; set; }
    }

    public sealed class ResourcePackDef
    {
        [JsonProperty("bracket")] public string Bracket { get; set; }
        [JsonProperty("gold_per_100wb")] public int GoldPer100Wb { get; set; }
        [JsonProperty("oil_per_100wb")] public int OilPer100Wb { get; set; }
        [JsonProperty("steel_per_100wb")] public int SteelPer100Wb { get; set; }
    }

    public sealed class ResourcePackRules
    {
        [JsonProperty("fills_free_storage_only")] public bool FillsFreeStorageOnly { get; set; }
        [JsonProperty("per_resource_cooldown_hours")] public int PerResourceCooldownHours { get; set; }
    }

    public sealed class CosmeticDef
    {
        [JsonProperty("item")] public string Item { get; set; }
        [JsonProperty("wb")] public int Wb { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
    }

    public sealed class FreeWarBondSource
    {
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("wb")] public int Wb { get; set; }
        [JsonProperty("frequency")] public string Frequency { get; set; }
    }

    public sealed class LootCapDef
    {
        [JsonProperty("cc")] public int Cc { get; set; }
        [JsonProperty("gold")] public int Gold { get; set; }
        [JsonProperty("oil")] public int Oil { get; set; }
        [JsonProperty("steel")] public int Steel { get; set; }
    }

    public sealed class OfferDef
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("price_usd")] public double PriceUsd { get; set; }
        [JsonProperty("contents")] public string Contents { get; set; }
        [JsonProperty("shown_when")] public string ShownWhen { get; set; }
        [JsonProperty("limit")] public string Limit { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
    }

    /// <summary>A per-resource fraction (Gold / Oil / Steel), e.g. steal rates.</summary>
    public sealed class ResourceRates
    {
        [JsonProperty("gold")] public double Gold { get; set; }
        [JsonProperty("oil")] public double Oil { get; set; }
        [JsonProperty("steel")] public double Steel { get; set; }
    }
}
