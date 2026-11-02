"""Cross-checks game data and workbooks. Run: python3 validate.py <package_dir>. Exit 1 on any failure."""
import json, sys, glob, os, re, zipfile

P = sys.argv[1] if len(sys.argv) > 1 else "."
GD = P + "/game_data/"
WB = P + "/design_workbooks/"
fails, passes = [], []


def check(ok, msg):
    (passes if ok else fails).append(msg)
    print(("PASS " if ok else "FAIL ") + msg)


units = json.load(open(GD + "units.json"))
tech = json.load(open(GD + "tech_tree.json"))
leaders = json.load(open(GD + "leaders.json"))
gens = json.load(open(GD + "generals.json"))
eco = json.load(open(GD + "economy.json"))
const = json.load(open(GD + "constants.json"))
NAT = const["nations"]

# ---- roster
check(len(units) == 175, "175 vehicles")
for n in NAT:
    us = [u for u in units if u["nation"] == n]
    check(len(us) == 35 and len({u["name"] for u in us}) == 35, f"{n}: 35 unique vehicles")
    for t in range(1, 6):
        tu = [u for u in us if u["tier"] == t]
        g = sum(1 for u in tu if u["domain"] == "ground")
        a = sum(1 for u in tu if u["domain"] == "air")
        check(len(tu) == 7 and g == 5 and a == 2, f"{n} tier {t}: 5 ground + 2 air")
check(all(u["hp"] > 0 for u in units), "every unit has HP > 0")
check(all(u["housing"] > 0 and u["oil"] > 0 and u["gold"] > 0 for u in units), "every unit has housing, oil, gold")
check(len({u["id"] for u in units}) == 175, "unit ids unique")
# monotonic power within nation by tier (average HP)
for n in NAT:
    avg = []
    for t in range(1, 6):
        v = [u["hp"] for u in units if u["nation"] == n and u["tier"] == t and u["domain"] == "ground"]
        avg.append(sum(v) / len(v))
    check(all(a < b for a, b in zip(avg, avg[1:])), f"{n}: ground HP rises each tier")
    avg = []
    for t in range(1, 6):
        v = [u["gold"] for u in units if u["nation"] == n and u["tier"] == t]
        avg.append(sum(v) / len(v))
    check(all(a < b for a, b in zip(avg, avg[1:])), f"{n}: unit Gold cost rises each tier")

# ---- tech tree
uid = {u["id"]: u for u in units}
tt = tech["units"]
check(len(tt) == 175 and {t["unit_id"] for t in tt} == set(uid), "tech tree covers exactly the 175 roster units")
ok = True
for t in tt:
    u = uid[t["unit_id"]]
    if u["nation"] != t["nation"] or u["tier"] != t["tier"]:
        ok = False
    if t["prerequisite_id"]:
        p = uid[t["prerequisite_id"]]
        if p["nation"] != u["nation"] or p["tier"] > u["tier"] or t["prerequisite_id"] == t["unit_id"]:
            ok = False
            print("  bad prereq", t)
check(ok, "tech tree: tier/nation match roster, prerequisites valid and not later-tier")
check(all(t["prerequisite_id"] or t["prerequisite_rule"] for t in tt), "every unit has a prerequisite or rule")
for n in NAT:
    first = [t for t in tt if t["nation"] == n and t["tier"] == 1 and not t["prerequisite_id"]]
    check(len(first) == 1, f"{n}: exactly one root unit")
# prerequisite chains reach root
def reach(t):
    seen = 0
    cur = t
    while cur["prerequisite_id"] and seen < 60:
        cur = next(x for x in tt if x["unit_id"] == cur["prerequisite_id"])
        seen += 1
    return seen < 60
check(all(reach(t) for t in tt), "all prerequisite chains terminate")
check(tech["tier_rp"]["5"] > tech["tier_rp"]["4"] > tech["tier_rp"]["3"] > tech["tier_rp"]["2"] > tech["tier_rp"]["1"], "RP per tier rises")

# ---- commanders
check(len(leaders) == 25 and all(sum(1 for l in leaders if l["nation"] == n) == 5 for n in NAT), "25 leaders (5 per nation)")
check(len(gens) == 35 and all(sum(1 for g in gens if g["nation"] == n) == 7 for n in NAT), "35 generals (7 per nation)")
names = [l["name"] for l in leaders] + [g["name"] for g in gens]
check(len(names) == len(set(names)), "no commander appears twice (Abrams fix)")
bad = [g["name"] for g in gens if "nfantry" in json.dumps(g) or "Naval" in g["c1"] + g["c2"]]
check(not bad, f"no infantry/naval generals {bad}")
check(not any("Nishiyama" in json.dumps(g) for g in gens), "fictional general removed")
check(all(g["aura"] and g["range"] for g in gens), "generals have aura and range")

# ---- economy
sr = eco["steal_rates"]
check((sr["gold"], sr["oil"], sr["steel"]) == (0.2, 0.25, 0.15), "steal rates 20/25/15")
check(const["steal_rates"] == sr, "constants steal rates match economy")
packs = eco["packs"]
check(all(a["price_usd"] < b["price_usd"] and a["total_wb"] < b["total_wb"] for a, b in zip(packs, packs[1:])), "packs rise in price and WB")
wbp = [p["total_wb"] / p["price_usd"] for p in packs]
check(all(a <= b for a, b in zip(wbp, wbp[1:])), "bigger packs never give fewer WB per dollar")
check(all(p["first_purchase_wb"] == p["total_wb"] + p["base_wb"] for p in packs), "first purchase bonus arithmetic")
sp = eco["speedup_table"]
check(all(a["wb"] <= b["wb"] for a, b in zip(sp, sp[1:])), "speed-up cost never decreases with time")
check(all(b["wb"] / b["minutes"] <= a["wb"] / a["minutes"] + 1e-9 for a, b in zip(sp, sp[1:])), "longer speed-ups cost less per minute")
check(all(a["gold"] < b["gold"] for a, b in zip(eco["loot_caps"], eco["loot_caps"][1:])), "loot caps rise with CC")
check("research_points" in eco["never_sold"], "RP stays unsellable")

# ---- buildings / tables
bt = json.load(open(GD + "tables_04_Buildings.json"))
cnt = {r["Building"]: r for r in bt["Building Count by CC"][next(iter(bt["Building Count by CC"]))]}
db = bt["Defense Buildings"]
rows = next(v for k, v in db.items() if k != "_notes")
okd = True
for r in rows:
    c = cnt.get(r["Building Name"])
    if not c:
        continue
    first = next(i for i in range(1, 11) if c[f"CC {i}"] not in ("-", 0, None))
    if r["Unlocked CC"] != f"CC {first}":
        okd = False
        print("  mismatch", r["Building Name"])
    mx = max(int(v) for k, v in c.items() if k.startswith("CC ") and v not in ("-", None))
    if r["Max Count"] != f"x{mx}":
        okd = False
        print("  count mismatch", r["Building Name"], r["Max Count"], mx)
check(okd, "defense unlock CC and max count match Building Count table")
walls = [r for r in next(v for k, v in bt["Walls"].items() if k != "_notes") if str(r.get("Wall Tier", "")).startswith("Tier")]
check(len(walls) == 10 and all(a["HP per Section"] < b["HP per Section"] for a, b in zip(walls, walls[1:])), "wall HP rises every tier")
check(all(a["Cost per Tile G"] < b["Cost per Tile G"] for a, b in zip(walls, walls[1:])), "wall Gold cost rises every tier")
check(not any("Cannot be destroyed" in json.dumps(walls[6]) for _ in [0]), "Dragon Teeth no longer 'cannot be destroyed'")
ccs = next(v for k, v in bt["Command Center"].items() if k != "_notes")
tier_cc = {"Tier I": 1, "Tier II": 3, "Tier III": 5, "Tier IV": 7, "Tier V": 9}
okc = True
for r in ccs:
    lv = int(r["CC Level"].split()[1])
    mt = r["Max Unit Tier"]
    need = tier_cc[mt]
    if need > lv or (lv < 9 and mt == "Tier V"):
        okc = False
nxt = {1: "Tier I", 2: "Tier I", 3: "Tier II", 4: "Tier II", 5: "Tier III", 6: "Tier III", 7: "Tier IV", 8: "Tier IV", 9: "Tier V", 10: "Tier V"}
okc = okc and all(r["Max Unit Tier"] == nxt[int(r["CC Level"].split()[1])] for r in ccs)
check(okc, "Command Center max unit tier matches tier CC requirements")
tu = json.load(open(GD + "tech_tree.json"))["tier_unlock"]
t5 = [r for r in next(iter(tu.values())) if str(r["Tier"]).startswith("TIER V")][0]
check("CC9" in t5["CC Required"], "Tier V requires CC9 in tech tree")
check(const["tier_requires_cc"]["V"] == 9, "constants say Tier V needs CC9")

# ---- workbooks
import openpyxl
nonascii = 0
nsheets = 0
for f in sorted(glob.glob(WB + "*.xlsx")):
    z = zipfile.ZipFile(f)
    check(z.testzip() is None, f"zip ok {os.path.basename(f)}")
    wb = openpyxl.load_workbook(f)
    for ws in wb.worksheets:
        nsheets += 1
        for row in ws.iter_rows(values_only=True):
            for c in row:
                if isinstance(c, str) and any(ord(ch) > 127 for ch in c):
                    nonascii += 1
        check(ws.max_row > 1, f"{os.path.basename(f)}/{ws.title} has data")
check(nonascii == 0, f"all workbook text ASCII-safe ({nonascii} bad cells)")
print(f"\n{len(passes)} passed, {len(fails)} failed, {nsheets} sheets read")
sys.exit(1 if fails else 0)
