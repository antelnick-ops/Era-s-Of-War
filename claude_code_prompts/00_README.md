# How to use these prompts

1. Open Claude Code in your game repo folder.
2. Copy the data package into the repo: `game_data/`, `design_workbooks/`, `docs/`, `tools/`.
3. Paste prompts one at a time, in order (01, 02, ...). Finish and test each before the next.
4. After each prompt: open Unity, press Play, check the acceptance list, commit to GitHub.
5. If anything is unclear, Claude Code should stop and ask rather than guess.

Rules every prompt follows:
- Game rules go in the `Simulation` C# library (no UnityEngine inside). Unity code only displays and handles touch.
- Numbers come from `game_data/*.json`, never hard-coded.
- Run `python3 tools/validate.py .` after any data change.
- Small commits with clear messages. No secrets in the repo.
