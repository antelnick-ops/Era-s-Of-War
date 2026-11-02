# Build Plan

## Tools (recommended)
| Need | Choice | Why |
|---|---|---|
| Game engine | **Unity 6 LTS, 2D (URP), C#** | Best mobile tooling and tutorials for this genre; Claude Code writes C# well. Unity Personal is free up to a revenue limit - check unity.com for the current figure. The 2023 runtime fee was cancelled in 2024. |
| Backend | **Supabase** (Postgres, Auth, Edge Functions) | You already have a Supabase setup for another project. Server decides all money, resources and receipts. |
| Purchases | Unity IAP, or RevenueCat | Handles App Store and Google Play billing. |
| Code | GitHub + Claude Code | Same flow as your other projects. |
| Art | Placeholder shapes first, then an artist or licensed packs | Do not copy War Thunder art. |

Alternatives considered: Godot (free and light but fewer mobile purchase plugins), Flutter + Flame (easy purchases, weaker game tooling).

## Architecture in one paragraph
All game rules live in a plain C# **Simulation** library (no Unity code in it) so Claude Code can build and test it from the command line. Unity is only the display and touch controls on top. Game data loads from the JSON files in `game_data/`. Later, the server runs the same rules to check raids.

## Milestones (part-time estimates, about 10-15 hrs/week)
| # | Milestone | Done when | Estimate |
|---|---|---|---|
| M0 | Setup | Unity project, GitHub repo, data imported, simulation test project runs | 1-2 weeks |
| M1 | Base grid | Place, move, upgrade buildings on a grid, saved locally | 2 weeks |
| M2 | Economy | Collectors, storage, timers, Engineer Crews | 2 weeks |
| M3 | Army | Barracks queue, Army Camp housing, 14 Germany Tier I-II units | 2 weeks |
| M4 | Raid vs AI base | 3-minute raid, formation, defenses fire, win rules | 4 weeks |
| **Vertical slice** | M0-M4 playable on your phone | **about 3 months** | |
| M5 | Accounts + cloud save | Supabase login, server-side resources | 3 weeks |
| M6 | All content | All 175 units, tech tree, commanders from JSON | 4 weeks |
| M7 | Async PvP | Matchmaking, shield, loot caps, trophies | 6 weeks |
| M8 | Purchases + polish | War Bonds, packs, pass, tutorial, soft launch | 6 weeks |

Soft launch is realistically 9-12 months part-time. Treat dates as estimates; the vertical slice tells you how fast you really go.

## Vertical slice scope (build this first)
- Germany only, Tiers I and II (14 vehicles), CC1 to CC3.
- Buildings: CC, Gold Mine, Oil Derrick, Steel Mill, Barracks, Army Camp, Cannon Tower, Machine Gun Nest, walls.
- One deployment zone, ground units only, vs an AI-generated base.
- No accounts, ads or purchases. If this is fun, build the rest.

## Costs to plan for
Apple Developer $99/year, Google Play $25 one-time, Supabase free tier to start then paid as players grow, art and sound (biggest unknown), trademark and legal review before launch. Store fee 15% for small developers if you enroll in Apple's Small Business Program and Google's reduced-fee program.

## Risks
1. **Scope.** 175 units is huge. Slice first.
2. **IP.** Original name and art; no Gaijin assets. Legal check before launch.
3. **Cheating in PvP.** Server must own resources; raids get verified or capped.
4. **Balance.** All numbers are first drafts; playtest and use the workbooks to retune.
5. **Art.** Placeholder shapes first; budget for real art before soft launch.
