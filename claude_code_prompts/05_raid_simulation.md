# Prompt 05 - Raid simulation (the core of the game)

Context: `tables_06_AttackMechanics.json`, `constants.json`, `units.json`, defenses from tables_04.

Steps:
1. Simulation: deterministic raid simulation on a fixed time step (seeded random). Inputs: attacker formation, defender base. Output: result, stolen resources, trophy change, a replay log.
2. Ground units follow target priority, pathfind around walls, breach walls; defenses target by their rules; 3-minute timer; air launch at 50% ground losses; win rules from Raid Economy.
3. Oil spent at launch, refund by outcome. Loot = min(steal rate x lootable, loot cap).
4. Unity: formation planner, raid view with simple sprites, timer, retreat button, results screen.
5. Vertical slice: one deployment zone and an AI-generated defender base. Add the other zones after it works.
6. Tests: same seed gives same result; steal and cap arithmetic; timeout and defeat cases.

Acceptance: you can plan, run and win or lose a raid, and the same seed always replays the same way.
