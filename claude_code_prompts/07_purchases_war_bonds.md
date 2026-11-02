# Prompt 07 - War Bonds and purchases

Context: `docs/TopUp_Options.md`, `game_data/economy.json`.

Steps:
1. Create products in App Store Connect and Google Play Console from `economy.json` packs and offers (I do the console work; list the exact product IDs and prices you need).
2. Unity IAP (or RevenueCat): purchase flow, restore purchases, pending purchases.
3. Supabase Edge Function `validate_purchase`: verify the receipt with Apple/Google, grant War Bonds exactly once (idempotent by transaction ID), record in `purchases`.
4. Shop UI: top-up packs, first-purchase bonus, speed-up button on timers using the cost curve, Engineer Crew purchase, shield purchase, resource packs with caps and 24h cooldown.
5. Offers popup rules: max one per session, never during a raid or right after a defeat.
6. Use test purchases only (sandbox/test tracks). No real money in testing.

Acceptance: sandbox purchase of every pack grants the right WB once; replaying the same receipt grants nothing; spend never makes a player skip a tier gate.
