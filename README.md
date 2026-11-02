# Eras of War

Mobile base-builder (Unity 6 + C#). Design package overview: [README_START_HERE.md](README_START_HERE.md).

## Layout
| Path | What |
|---|---|
| `Era's of War/` | Unity 6 (URP) project. Display and touch only. |
| `Simulation/` | Plain C# game-rules library (netstandard2.1, C# 9). **No UnityEngine.** |
| `Simulation.Tests/` | NUnit tests for `Simulation` (`net10.0`). |
| `game_data/` | JSON the game reads. Generated from `design_workbooks/`. |
| `design_workbooks/` | WTBB_01..10 Excel files: design source of truth. |
| `docs/`, `claude_code_prompts/` | Design docs and the build prompts, in order. |
| `tools/validate.py` | Cross-checks game data and workbooks. |

## Requirements
- .NET SDK 10 (LTS): `winget install Microsoft.DotNet.SDK.10`
- Unity 6000.6.7f1 (via Unity Hub)
- Python 3 + `pip install openpyxl` (for the validator)

## Run the tests
From the repo root:

```
dotnet test
```

This builds `Simulation`, runs `Simulation.Tests`, and copies `Simulation.dll` into
`Era's of War/Assets/Plugins/Simulation/` so Unity picks up the latest rules.

Unity Play Mode smoke test (Unity must be closed, or use Window > General > Test Runner > PlayMode):

```
"C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -projectPath "Era's of War" -runTests -testPlatform PlayMode -testResults playmode-results.xml
```

It loads the Base scene and presses HUD buttons; any error or exception fails it. Your real save is moved aside and restored.

## Validate game data
After any change to `game_data/` or `design_workbooks/`:

```
python tools/validate.py .
```

Exit code 0 means all checks pass.

## Open in Unity
1. Run `dotnet build` once (the copied `Simulation.dll` is not committed).
2. Open `Era's of War/` in Unity Hub.
3. Press Play. The Console shows
   `[Eras of War] Game data loaded: 175 units (Germany 35, USSR 35, USA 35, Britain 35, Japan 35), ...`

In the editor, data is read straight from the repo's `game_data/`. Device builds will read it from
StreamingAssets (a later milestone).

## Base screen (Prompt 02)
- Scene: `Assets/Scenes/Base.unity` (first in Build Settings). URP **2D Renderer** (`Assets/Settings/Renderer2D.asset`),
  orthographic camera, portrait only. Re-create with **Eras of War > Set Up 2D Base Scene** (safe to re-run).
- Rules (bounds, overlap, CC gating, count limits, wall tiles, max level) are in `Simulation/Base/` and tested.
  Unity (`Assets/Scripts/Base/`) only draws colored squares and turns touches into requests.
- Save file: `base.json` in `Application.persistentDataPath`
  (editor on Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Eras of War\`). Delete it or use **Reset base** to start over.

## How Unity uses Simulation
`Simulation.csproj` has a post-build step that copies `Simulation.dll` (+ pdb) into
`Assets/Plugins/Simulation/`. Unity auto-references it. JSON parsing uses Newtonsoft.Json:
the NuGet package for `dotnet`, and Unity's `com.unity.nuget.newtonsoft-json` package in the editor
(same assembly version, so one DLL works in both). `Assets/link.xml` stops IL2CPP from stripping the
data classes.

Rules: game logic goes in `Simulation`; numbers come from `game_data/*.json`, never hard-coded.
