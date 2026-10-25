"""Exports Data/catalog.json from SPEC.md tables so the C# core and the spec cannot drift apart.

Usage: python -X utf8 export_catalog.py
"""
import json, re, os

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = os.path.join(HERE, "..", "..", "SPEC.md")
OUT = os.path.join(HERE, "..", "Data", "catalog.json")

spec = open(SPEC, encoding="utf-8").read().splitlines()
FA = str.maketrans("۰۱۲۳۴۵۶۷۸۹", "0123456789")
num = lambda s: s.translate(FA)


def parse(prefix):
    rx = re.compile(r"^\| `(%s\d{2})` \| \*\*(.+?) \(`(.+?)`\)\*\*(.*)$" % prefix)
    out = {}
    for l in spec:
        m = rx.match(l)
        if m:
            out[m.group(1)] = (m.group(2), m.group(3), m.group(4))
    return out


traps_raw, mons_raw, rw_raw, mu_raw = parse("T"), parse("M"), parse("R"), parse("MU")
assert (len(traps_raw), len(mons_raw), len(rw_raw), len(mu_raw)) == (90, 22, 37, 13)

# ---- traps ---------------------------------------------------------------
HOME = {}
for i in range(1, 9): HOME[f"T{i:02d}"] = 2
for i in range(9, 17): HOME[f"T{i:02d}"] = 3
for i in range(17, 25): HOME[f"T{i:02d}"] = 4
for i in range(25, 31): HOME[f"T{i:02d}"] = 5
HOME.update({"T31": 6, "T32": 6, "T33": 8, "T34": 7, "T35": 7, "T36": 8, "T37": 8, "T38": 10, "T39": 7, "T40": 8})
for i in range(41, 47): HOME[f"T{i:02d}"] = 9
for i in range(47, 61): HOME[f"T{i:02d}"] = 0  # universal mechanical family (also floor-1 genre)
HOME["T58"] = 6
UPDATE1 = {"T38", "T90"}

BYPASS = {"T12", "T31", "T32", "T45", "T88"}  # permanent areas: never on the golden path
TIMED = {"T25": 1.5, "T29": 2.0, "T34": 1.5, "T38": 1.5, "T47": 1.5, "T50": 1.5, "T51": 2.0, "T52": 1.5,
         "T62": 1.5, "T68": 1.5, "T81": 1.5}
FULLROW = {"T25", "T29", "T34", "T47", "T51", "T52", "T62", "T68", "T81"}
INTERACTIVE = {"T57", "T58", "T59", "T60"}
TIER_PD = {"S", "A", "B", "C"}

traps = []
for code, (fa, en, rest) in sorted(traps_raw.items()):
    cells = [c.strip() for c in rest.split("|")]
    n = int(code[1:])
    if n >= 61:
        fl, tier = [x.strip() for x in cells[1].split("/")]
        home = int(num(fl)); tier = tier if tier in TIER_PD else "A"
    else:
        home = HOME[code]
        m = re.search(r"-(\d+) HP", num(cells[1]))
        d = int(m.group(1)) if m else 0
        tier = "C" if d >= 31 else "B" if d >= 16 else "A" if d >= 1 else ("A" if re.search(r"Stun|ثانیه|معکوس|تار", cells[1]) else "S")
    policy = "BlocksWithBypass" if code in BYPASS else "TimedBlocks" if code in TIMED else "NeverBlocks"
    traps.append(dict(id=code, nameEn=en, nameFa=fa, homeFloor=home, tier=tier, policy=policy,
                      shape="FullRow" if code in FULLROW else "Tile", safeWindow=TIMED.get(code, 0.0),
                      interactive=code in INTERACTIVE, release="update1" if (code in UPDATE1 or home >= 7) else "v1.0"))

# ---- rewards -------------------------------------------------------------
RARITY = {}
for c in "R01 R02 R06 R10 R13 R14 R15 R25 R31".split(): RARITY[c] = "Common"
for c in "R03 R05 R07 R09 R11 R16 R19 R20 R22 R23 R28 R30 R32 R34".split(): RARITY[c] = "Rare"
for c in "R04 R08 R12 R17 R18 R21 R24 R26 R27 R29 R33 R35 R36".split(): RARITY[c] = "Epic"
RARITY["R37"] = "Legendary"
assert set(RARITY) == set(rw_raw), set(rw_raw) ^ set(RARITY)
rewards = [dict(id=c, nameEn=rw_raw[c][1], nameFa=rw_raw[c][0], rarity=RARITY[c], release="update1" if c == "R37" else "v1.0")
           for c in sorted(rw_raw)]

# ---- monsters ------------------------------------------------------------
BOSS = {"M02", "M05", "M08", "M10", "M12", "M14", "M16", "M18", "M20", "M22"}
monsters = []
for c, (fa, en, rest) in sorted(mons_raw.items()):
    m = re.search(r"طبقه (\d+)", num(rest))
    floor = int(m.group(1)) if m else 10
    monsters.append(dict(id=c, nameEn=en, nameFa=fa, floor=floor, boss=c in BOSS,
                         release="v1.0" if (floor <= 6 and c != "M14") or c == "M22" else "update1"))

# ---- mutators ------------------------------------------------------------
mutators = []
for c, (fa, en, rest) in sorted(mu_raw.items()):
    cells = [x.strip() for x in rest.split("|")]
    # cells: ['', effect, floors, conflicts, gauntletSafe, '']
    floors = [int(x) for x in re.findall(r"\d+", num(cells[2]))]
    conflicts = re.findall(r"[TR]\d{2}", cells[3])
    mutators.append(dict(id=c, nameEn=en, nameFa=fa, floors=floors, conflicts=conflicts,
                         gauntletSafe="✓" in cells[4], release="update1" if c in ("MU12", "MU13") else "v1.0"))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(dict(traps=traps, rewards=rewards, monsters=monsters, mutators=mutators), open(OUT, "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
print("traps", len(traps), "v1.0:", sum(t["release"] == "v1.0" for t in traps),
      "| rewards", len(rewards), "| monsters", len(monsters), "| mutators", len(mutators))
