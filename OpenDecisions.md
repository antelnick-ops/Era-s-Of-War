# Open Decisions - need your approval

Things that were fixes with a judgment call, or new rules I had to add so the game works. Say "approved" or tell me what to change. Nothing here blocks starting the vertical slice, but 1-3 affect the economy numbers.

## New rules added (not in earlier designs)
1. **Engineer Crews (builders).** The design had upgrade timers but no limit on how many things build at once. Proposed: 2 free crews, crew 3 (CC3) = 250 WB, crew 4 (CC5) = 500 WB, crew 5 (CC7) = 1,000 WB. Free players can earn crew 3 in about a month.
2. **RP multiplier by Command Center.** The old RP pacing would have taken about 200 days to finish one nation. Now RP earned is multiplied x1.0 / 1.25 / 1.5 / 2.0 / 2.5 by CC bracket. One nation takes about 113 days casual or 55 days active (RP time only). Alternative: lower the RP costs instead.
3. **Housing and Oil per unit.** Every unit now has Housing (Light 2 up to Jet Attack 9) and an Oil cost (Gold cost x class factor). Needed for Army Camps and Oil to work.
4. **Loot cap + protected storage.** Replaces the "10% of reserves" rule that contradicted the steal rates. Each storage protects 10% of capacity and each raid is capped by defender CC.
5. **War Bonds premium currency** and all purchase prices (see TopUp_Options.md).

## Fix choices to confirm
6. **Air HP.** All air units had 0 HP. Set to DPS x 6 (fighters, jets) or x 8 (attack, bombers). Helicopters kept their HP.
7. **USA Tier IV.** Had 4 ground + 3 air. Swapped the A-6 Intruder for the M551 Sheridan (Light Tank).
8. **USA M19 MGMC duplicate.** Tier II renamed M16 MGMC.
9. **Tier V needs CC9** (was CC8 in one file). Dark Barracks still builds at CC8.
10. **Tier V trains only in Dark Barracks.** Barracks and Air Hangar top out at Tier IV.
11. **Dragon Teeth** is now destructible (22,000 HP, immune to splash). HESCO raised to 26,000 HP so walls always get stronger.
12. **Generals:** infantry generals changed to a new general-only class called Special (Student, Gavin: air-landed Light Tanks). Nishiyama (fictional) replaced by Baron Takeichi Nishi. Abrams duplicate replaced by John S. Wood. Kurita is now SPG with an off-map naval gunfire call.
13. **Kenryo Sato** is a weak historical fit. His ability is now a generic fire plan. Keep, or give me a better Japanese commander.
14. **Tier labels.** Tiers are power tiers, not strict history (Tiger II sits in "Cold War" Tier IV). Fine for gameplay; rename tiers if it bugs you.
15. **Defense towers.** The 28 real-world systems are now nation skins on the 16 defense buildings. Ground towers (Cannon, Mortar, Sniper, Bomb, Flame) have no real-world skins yet.

## Build decisions I need you to confirm
16. **Engine: Unity 6 LTS + C#.** Alternatives: Godot (lighter, fewer mobile purchase tools), Flutter+Flame (simpler but weaker for game feel).
17. **Backend: Supabase** (you already have a Supabase setup for another project). Not needed until milestone 5.
18. **Name and IP.** Do not use "War Thunder" or any Gaijin art, logos or text. Pick an original game name. Real vehicle names are historical, but have an IP lawyer check manufacturer and brand names, logos and your game name before launch. This is not legal advice.
19. **Age rating 13+** and no loot boxes.
