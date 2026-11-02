# Eras of War - Build Package

Everything from the design phase, fixed and packaged so you can start building.

## What is in here
| Folder | What it is |
|---|---|
| `design_workbooks/` | 10 Excel files (open on your phone). WTBB_01 to WTBB_10. These are the source of truth for design. |
| `game_data/` | The same data as JSON for the game to read: 175 units, tech tree, 25 leaders, 35 generals, buildings, economy, constants. |
| `docs/` | Master design doc, build plan, top-up options, fix log, open decisions. |
| `claude_code_prompts/` | Step-by-step prompts to paste into Claude Code, in order. |
| `tools/validate.py` | Checks that all the data agrees with itself. Run `pip install openpyxl` once, then `python3 tools/validate.py .` from this folder any time you edit data. |

## Read in this order
1. `docs/OpenDecisions.md` - things I changed or added that need your yes/no
2. `docs/GDD_MasterDesign.md` - the whole game on a few pages
3. `docs/TopUp_Options.md` - in-game purchases and War Bonds
4. `docs/BuildPlan.md` - tools, milestones, first playable
5. `claude_code_prompts/00_README.md` - how to start building

## Status
- 175 vehicles, all with HP, Housing and Oil cost. Validator: all checks pass.
- 26 fixes applied (see `docs/FixLog.md`).
- Old versions (v1/v2/v3 files) are superseded by the WTBB_ files and moved to `_superseded`.
