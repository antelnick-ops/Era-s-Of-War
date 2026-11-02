# Fix Log

29 fixes applied across the design workbooks. Cross-checks that confirm them are in `tools/validate.py`.

| Area | Problem found | Fix |
|---|---|---|
| Roster | USA Tier IV had 4 ground + 3 air units instead of 5 ground + 2 air. | A-6 Intruder replaced by M551 Sheridan (Light Tank) so all 25 tiers are 5 ground + 2 air. Jet Attack returns at Tier V with the A-10. |
| Roster | USA had M19 MGMC listed twice (Tier II and Tier III). | Tier II unit renamed to M16 MGMC (real quad-.50 half-track); Tier III keeps M19 MGMC. |
| Roster | All 45 air units (fighters, ground attack, jets) had HP = 0 so they could not be damaged. | Air HP set from DPS x class factor (see Overview). Helicopters already had HP. |
| Roster | No Housing or Oil values per unit, so Army Camp capacity and Oil cost could not be applied. | Added Housing and Oil per Deploy columns to every unit (rules on Overview tab). |
| Roster | Roster contained emoji characters, a corruption risk on mobile Excel. | Removed all emoji; file rebuilt with xlsxwriter. |
| Tech tree | RP pacing: old estimate said Tier V takes ~85 days casual, but real income (~375 RP/day) would take ~200 days, and the tab's tier totals (2900/6650/16100/34600/75500) did not match the trees (3,050/6,600/16,300/35,600/77,000). | Rebuilt RP Earn Rates as a formula model: raid outcome mix, daily income, and an RP multiplier by CC bracket (x1 to x2.5). Result: about 113 days casual / 55 days active to finish one nation. All inputs editable. |
| Tech tree | Unit names differed from roster (e.g. Bison SPG, Typ 2000 TD, Type 92 SPG, Ka-50, Type 88 AA). | All 9 mismatched names normalized to the roster; validator confirms every tech-tree unit exists in the roster and vice versa. |
| Tech tree | Tier V gate said CC8 but Buildings workbook gives Tier V at CC9. | Tier V gate is now CC9 + Dark Barracks everywhere. |
| Tech tree | Tier header RP totals did not match the unit rows. | Totals and running totals are recomputed from unit rows. |
| Leaders | Omar Bradley's ability referred to infantry, which do not exist. | Now halves Oil cost of Medium Tanks. |
| Generals | Four infantry-class generals (Kurt Student, John Gavin, Matthew Ridgway, Orde Wingate) in a game with no infantry. | Student and Gavin are now Light Tank / Special (air-landed Light Tanks). Ridgway is Heavy/Medium Tank. Wingate is Light Tank/Tank Destroyer. 'Special' is a general-only class. |
| Generals | Hiroshi Nishiyama (Japan) is not a real person. | Replaced with Takeichi Nishi (Baron Nishi), real commander of the 26th Tank Regiment at Iwo Jima. |
| Generals | Creighton Abrams was both a USA leader and a general. | General replaced with John S. Wood (real 4th Armored Division commander). Abrams remains the leader. |
| Generals | Kurita was class Naval but there are no naval units. | Now SPG / Heavy Tank with an off-map naval gunfire ability. |
| Generals | Kenryo Sato's ability (intelligence chief) was a stretch for the historical record. | Ability changed to generic fire-plan suppression and flagged as a placeholder in Open Decisions. |
| Generals | General Comparison tab had mislabeled columns: 'Armor' showed range and 'Aura Radius' showed armor. Per-nation aura text was generic. | Columns relabeled to Range and Armor with an Aura Buff column; every general now shows a concrete aura (+10% DPS and +10% HP to its two classes within 2 tiles). |
| Defenses | Defenses workbook listed 28 real-world systems while the Buildings workbook listed 16 defense buildings with different unlock levels. | Buildings is now the single source of truth for gameplay stats and unlock levels. All 28 real systems are mapped to a Game Slot as nation skins (Defense Variant Map tab); each nation shows its own real-world systems. Defenses workbook is now reference only. |
| Buildings | Command Center tab described grid growth (+2x2, +4x4...) that did not match the Base Layout grid sizes, and its unlock lists did not match the Building Count table. | Both columns on the CC tab are now generated from the Building Count and Base Layout tables. |
| Buildings | Tier V trained in both Barracks Lv8 / Air Hangar Lv7-8 and the Dark Barracks. | Tier V (ground and air) now trains in the Dark Barracks only; Barracks and Hangar top out at Tier IV. |
| Buildings | Resource Buildings tab disagreed with the Resource System workbook (e.g. Gold Mine Lv1 HP 2000 vs 400, Oil Derrick upgrade costs). | Resource System workbook is the single source of truth; Buildings tab now only lists counts and points to it. |
| Buildings | Defense unlock levels and counts inside the Defense Buildings tab could disagree with the Building Count table. | Unlock CC and max count are now taken from the Building Count table. |
| Buildings | Dragon Teeth (Tier 7 wall) was described as 'cannot be destroyed' but had 22,000 HP, and Tier 8 HESCO had less HP (18,000) and was cheaper than Tier 7. | Dragon Teeth is destructible (22,000 HP) with splash immunity. HESCO raised to 26,000 HP, 15,000 Gold, 8,500 Steel so walls rise strictly in HP and cost. |
| Buildings | Patriot SAM was named 'Patriot SAM' in the Building Count table but 'Patriot SAM Battery' elsewhere, so the validator skipped it. | Renamed to 'Patriot SAM Battery' everywhere; its unlock CC and count are now checked like every other defense. |
| Buildings | Training building level tables were titled with notes ('Max count: ...') instead of building names. | Each table now has its building name as the title and the note as a separate Note row. |
| Buildings | No rule limited building level by Command Center level (a Gold Mine could reach Lv 8 at CC1). | New Building Level Caps tab and game_data/building_rules.json: collectors and storage max level = CC+1 (cap 8); Barracks and Air Hangar = CC (cap 8); Dark Barracks 2/4/5 at CC8/9/10; defenses = CC - unlock CC + 3 (cap per tower); Research Lab, Army Camp and Intel Center keep their per-level CC requirements. |
| Economy | Offline-protection cap (max 10% of reserves per raid) contradicted the steal rates of 20% Gold / 25% Oil / 15% Steel. | Replaced by one rule: loot = min(steal% x lootable, Loot Cap by defender CC) with 10% of each storage protected. Loot Cap table is in the Economy Monetization workbook. |
| Economy | Oil deployment cost table covered only 11 class/tier combinations, and units had no housing. | Oil is now a per-unit rule shown on every roster row; housing added per class. |
| Monetization | No in-game purchase design existed. | Added War Bonds (premium currency) with 6 top-up packs, speed-up curve, Engineer Crews, shields, capped resource packs, cosmetics, Starter Pack, War Pass and subscription, and a formula-driven revenue model. |
| Monetization | Design never defined builders (parallel construction), so upgrade timers had no limit. | Added Engineer Crews: 2 free, crews 3-5 unlock at CC3/5/7 and are bought with WB. This is a new rule needing your approval. |
