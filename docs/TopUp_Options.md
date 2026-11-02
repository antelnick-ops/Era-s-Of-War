# In-Game Purchases and Top-Ups

Free to play. One premium currency: **War Bonds (WB)**. Gold, Oil, Steel and RP keep working exactly as designed.

## Top-up packs (buy War Bonds)
| Pack | Price | Base WB | Bonus | Total WB | First purchase |
|---|---|---|---|---|---|
| Pocket Change | $0.99 | 99 | 0% | **99** | 198 |
| Ammo Crate | $4.99 | 499 | 10% | **549** | 1,048 |
| Supply Truck | $9.99 | 999 | 20% | **1,199** | 2,198 |
| Armored Convoy | $19.99 | 1,999 | 25% | **2,499** | 4,498 |
| War Chest | $49.99 | 4,999 | 30% | **6,499** | 11,498 |
| Strategic Reserve | $99.99 | 9,999 | 40% | **13,999** | 23,998 |

First purchase of each pack gives double base WB once (tracked on the server). Base rate is 100 WB per $1; every number is editable in `WTBB_10_EconomyMonetization.xlsx` (Top-Up Packs tab) and recalculates.

## What War Bonds buy
- **Speed-ups** (buildings, research, training, tier unlock timers):

| Time left | WB |
|---|---|
| 5 min | 3 |
| 30 min | 11 |
| 1 hr | 20 |
| 2 hr | 31 |
| 6 hr | 73 |
| 12 hr | 135 |
| 1 day | 260 |
| 2 day | 384 |
| 3 day | 507 |
| 4 day | 630 |
| 6 day | 877 |
| 7 day | 1,000 |

- **Engineer Crews:** crew 3 = 250 WB, crew 4 = 500, crew 5 = 1,000 (permanent).
- **Peace shields:** 1 day = 100 WB, 2 days = 150, 7 days = 400.
- **Resource packs:** per 100 WB, scaled by CC bracket, only fills free storage, once per resource per 24 h.
- **Cosmetics:** camos, base and wall skins, commander portraits, banners (250 to 1,200 WB).
- **Convenience:** +5 formation templates (150 WB), name change (200 WB).

## Bundles, pass and subscription
| Offer | Price | Contents | Shown when |
|---|---|---|---|
| Starter Pack | $2.99 | 300 WB + 50,000 Gold + 25,000 Oil + 8,000 Steel + 1 free speed-up token (1 hr) + Starter camo | After tutorial, CC2 |
| Engineer Bundle | $6.99 | Engineer Crew 3 + 150 WB + 12h shield token | CC3 reached |
| War Pass (Season Pass) | $4.99 | 30-day season. Premium track: 600 WB drip, exclusive camo, 3 resource packs, +5 formation templates, 2 shield tokens. Free track still exists. | Always in shop |
| Quartermaster Subscription | $4.99 | 20 WB per day for 30 days (600 WB) + 1 free 12h shield per week + badge. Cosmetic and convenience only. | Always in shop |
| Command Bundle | $19.99 | Choice of leader alternate portrait + nation camo set (1 tier) + 1,500 WB | CC5 reached |
| Comeback Pack | $3.99 | 200 WB + 100,000 resources scaled to CC + 24h shield | Returning after 14 days away |
| Event Bundle | $9.99 | Seasonal limited camo + 700 WB | Events |

## What money can NOT buy
Research Points, trophies, units directly, stat boosts, tier gates. Paying makes things faster or prettier; it does not open content a free player cannot reach.

## Free War Bonds
Tutorial 100, daily missions, league promotions, achievements and the free pass track give about 600 WB in the first 30 days, enough for Engineer Crew 3 without paying.

## Money flow
- Apple and Google both charge 15% (instead of 30%) on the first $1 million a year for small developers, if you apply for the programs. Net on a $9.99 pack is about $8.49.
- Revenue Model tab gives rough Low / Base / High monthly scenarios. Treat them as assumptions: most games earn less than the Low case, so measure with a small first release.

## Rules to follow
- No random paid boxes. Every purchase shows exactly what you get.
- All purchases through App Store / Google Play billing. Include Restore Purchases. Show local prices from the store.
- Never show an offer during a raid or right after a defeat. Maximum one popup per session.
- Server grants WB only after receipt validation. The phone never decides how many WB you own.
- Rate 13+. Add an optional spend limit in settings.

## Implementation (when you get to it)
Unity IAP (or RevenueCat) for purchases, Supabase Edge Function for receipt validation and WB grants. Prompt 07 in `claude_code_prompts/` covers it.
