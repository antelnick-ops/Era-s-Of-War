# Eras of War - Master Design Doc

Mobile base-builder in the style of Clash of Clans, with War Thunder-style vehicles and tech trees. Real historical vehicle names. No Gaijin art, text or logos.

## 1. Core loop
Build and upgrade a base -> collect Gold, Oil, Steel -> train vehicles -> raid other players' bases (3 minutes) -> steal resources and trophies -> research the next tier -> repeat. Defend your base automatically while offline.

## 2. Nations (5, no lock-in)
Germany (+15% armor), USSR (+20% HP, -10% cost), USA (+15% air effectiveness), Britain (+10% Tank Destroyer range), Japan (+20% speed, -10% HP). Each has 2 unique mechanics (see WTBB_08). A base can field any unlocked nation's vehicles.

## 3. Units
- 5 tiers x 7 vehicles x 5 nations = **175 vehicles** (5 ground + 2 air per tier). No infantry.
- Classes: Light, Medium, Heavy Tank, Tank Destroyer, SPG, AA Tank, Fighter, Ground Attack, Bomber, Jet Fighter, Jet Attack, Helicopter.
- Stats: HP, DPS, Speed, Range, Armor %, Gold cost, Train time, Housing, Oil per deploy, Target priority, Special ability. Data: `game_data/units.json`.
- Oil per deployed unit = round(Gold cost x class factor). Raid Oil is spent on launch, partly refunded on big wins.

## 4. Resources
Gold (everything), Oil (spent per raid deployment), Steel (construction). Steal rates 20% / 25% / 15% of lootable storage, limited by a Loot Cap by defender CC. 10% of each storage is protected. 12-hour shield after being raided, 3-day new-player shield. Collectors stop at 150% of local storage. Details: WTBB_03.

## 5. Command Center and buildings
CC has 10 levels and gates everything. Grid grows 20x20 to 40x40. Building types: Gold Mine, Oil Derrick, Steel Mill, Gold Vault, Oil Tank, Steel Depot, Barracks, Air Hangar, Dark Barracks, Research Lab, Army Camp, Intel Center, 16 defense types, walls (10 tiers, perimeter plus up to 200 placeable tiles), Alliance Hall, War Bunker, Alliance Research Lab. Counts by CC in WTBB_04 (Building Count by CC is the source of truth).
- Tier I needs CC1, Tier II CC3, Tier III CC5, Tier IV CC7, Tier V CC9 plus a Dark Barracks. Tier V trains only in the Dark Barracks.
- Free placement anywhere on the grid; a fixed perimeter wall plus placeable wall tiles.

## 6. Raids
- 4 deployment zones (N, S, E, W). Formation grid per zone (N/S 5x4, E/W 4x3 = 64 slots), locked at launch.
- 3-minute timer from first perimeter breach. Air units launch automatically when 50% of deployed ground units are destroyed.
- Results: Perfect (100%), Major (75%), Victory (Command Center destroyed), Partial (50%), Defeat, Timeout, with different steal, refund and trophy outcomes (WTBB_03 Raid Economy).
- Defender is offline; defenses and War Bunker garrison fight automatically.

## 7. Progression
Per-nation tech tree (WTBB_05). Tier gates need CC, Research Lab level, Gold + Steel, trophies and unlock time. Individual vehicles cost Research Points (earned only by playing). RP multiplier by CC: x1 to x2.5. One nation takes about 113 days casual / 55 days active in RP time. Prestige resets a nation for +5% stats and a cosmetic flag.

## 8. Commanders
- **Leaders:** 5 per nation (25), pick one: passive buff + one active ability per raid.
- **Generals:** 7 per nation (35), pick 3 for the roster, bring up to 2 per raid. They are standalone units placed in the formation grid with an aura (+10% DPS and HP to their classes within 2 tiles). Destroyed = Wounded for 1 raid.
- All are real historical commanders. 'Special' class = air-landed commander units.

## 9. Monetization
War Bonds, top-up packs, speed-ups, Engineer Crews, shields, capped resource packs, cosmetics, War Pass, subscription, no loot boxes, no sale of RP or tier gates. See TopUp_Options.md.

## 10. Data map
| Topic | Workbook | JSON |
|---|---|---|
| Units | WTBB_01 | units.json |
| Defense references | WTBB_02 | tables_02_Defenses_Reference.json |
| Resources and raids | WTBB_03 | tables_03_ResourceSystem.json |
| Buildings and walls | WTBB_04 | tables_04_Buildings.json |
| Tech tree and RP | WTBB_05 | tech_tree.json |
| Attack rules | WTBB_06 | tables_06_AttackMechanics.json, constants.json |
| Base layout | WTBB_07 | tables_07_BaseLayout.json |
| Nations | WTBB_08 | tables_08_NationSelection.json |
| Leaders and generals | WTBB_09 | leaders.json, generals.json |
| Economy and purchases | WTBB_10 | economy.json |

## 11. Not in scope for launch
Naval units, infantry, clans beyond Alliance Hall basics, live chat moderation tools, ads, real-time PvP.
