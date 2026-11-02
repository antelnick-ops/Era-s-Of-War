# Prompt 02 - Base grid and building placement

Goal: a base the player can build on.

Context: `game_data/constants.json` (grid by CC), `tables_04_Buildings.json`, `tables_07_BaseLayout.json` (footprints).

Steps:
1. Simulation: `BaseState` with grid size by CC, placed buildings (type, level, x, y, footprint), rules: no overlap, inside grid, count limits from Building Count by CC, wall tiles limit.
2. Simulation: place, move, remove, upgrade requests that return success or a clear error. No Unity types.
3. Unity: draw the grid, build menu, drag to place, tap to select, move mode. Use placeholder colored squares.
4. Save and load base as JSON on the device.
5. Tests for overlap, bounds, count limits, CC gating.

Acceptance: on phone you can place a CC, mines, and walls; limits enforced; reopen app and base is restored.
