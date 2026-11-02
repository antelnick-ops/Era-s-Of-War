# Prompt 01 - Project setup

Goal: create the repo structure and a working simulation test project.

Context: read `docs/GDD_MasterDesign.md` and `docs/BuildPlan.md`.

Steps:
1. Create a Unity 6 LTS 2D (URP) mobile project in `UnityGame/` (I will create it in Unity Hub and give you the folder; just add scripts).
2. Create `Simulation/` as a plain C# class library (netstandard2.1) with a test project `Simulation.Tests/` (NUnit). It must build and test with `dotnet test`.
3. Link the Simulation library into Unity (copy on build or via an assembly definition) so Unity can use it.
4. Add `Simulation/Data/` loaders that read `game_data/units.json`, `tech_tree.json`, `economy.json`, `constants.json`, `leaders.json`, `generals.json` into typed classes.
5. Add tests: 175 units load, 35 per nation, every unit has HP > 0, every tech tree entry references a real unit.
6. Add `.gitignore` for Unity, README with how to run tests.

Acceptance:
- `dotnet test` passes.
- Unity opens with no errors.
- Data loaders show 175 units in a debug log when Unity enters Play mode.

Stop and ask if: Unity version or folder layout is unclear.
