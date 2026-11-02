# Prompt 04 - Army, training, housing

Context: `units.json`, tables_04 Training Buildings and Support Buildings.

Steps:
1. Simulation: Barracks and Air Hangar queues (slots by level), train time and Gold cost per unit, Army Camp housing total, unit availability by tier and building level.
2. Respect: unit available only if tier researched (tech tree) and building level allows it.
3. Unity: training screen, queue with countdowns, army overview with housing used/total.
4. Start with Germany Tier I and II (14 units) unlocked for the vertical slice.
5. Tests for housing limits, queue slots, gating.

Acceptance: train several units, housing bar fills, queue blocks when full.
