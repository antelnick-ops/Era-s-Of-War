# Prompt 06 - Accounts and cloud save (Supabase)

Context: you have used Supabase before. New Supabase project for this game.

Steps:
1. Tables: players, bases, army, research, wallet (gold, oil, steel, war_bonds), purchases, raids, shields. Row Level Security: players can only read their own rows, write nothing directly.
2. Edge Functions (TypeScript): create_player, collect, start_upgrade, train_units, finish_timers, start_raid, finish_raid. All resource changes happen here, never on the phone.
3. Unity: sign in (anonymous first, link account later), call functions, show server state.
4. Store the server version of the same constants (copy of game_data JSON).
5. Tests for each function; a script that checks the game_data JSON on server equals the repo copy.

Acceptance: delete the app, reinstall, sign in, and your base and resources return. A modified phone cannot grant itself resources.
