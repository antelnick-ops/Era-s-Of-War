# Prompt 03 - Resources, collectors, timers, Engineer Crews

Context: `tables_03_ResourceSystem.json` (Passive Generation, Storage), `economy.json` (crews), `docs/GDD_MasterDesign.md`.

Steps:
1. Simulation: collectors produce per hour into local storage (cap 150% rule), player collects into vaults; vault capacity limits and 10% protected amount.
2. Upgrades cost Gold/Steel and take time; Engineer Crews limit parallel upgrades (2 free; more at CC3/5/7 purchasable later).
3. Offline progress: when app reopens, calculate time passed using server/UTC time with a clock-cheat guard (store last seen time, clamp negative jumps).
4. Unity: resource bar, collect taps, upgrade button with countdown, crew indicators.
5. Tests for generation, caps, timers, crews.

Acceptance: leave the app 10 minutes, return, resources and timers have advanced correctly; third upgrade is blocked until a crew is free.
