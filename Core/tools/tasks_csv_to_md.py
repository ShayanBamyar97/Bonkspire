"""Builds TASKS.md (readable backlog + capacity tables) from TASKS.csv, which is the source of truth.

  python -X utf8 Core/tools/tasks_csv_to_md.py

NOTE: the script that originally GENERATED TASKS.csv from the SPEC tables was not kept; edit TASKS.csv directly (or re-create the generator).
"""
import csv, os, collections

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
CSV = os.path.join(ROOT, "TASKS.csv")
TAIL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tasks_capacity_tail.md")
OUT = os.path.join(ROOT, "TASKS.md")

rows = list(csv.DictReader(open(CSV, encoding="utf-8-sig")))
ROLES = {"PROG": "برنامه‌نویس گیم‌پلی", "NETP": "برنامه‌نویس شبکه/بک‌اند", "UIP": "برنامه‌نویس UI/ابزار", "ART": "هنرمند سه‌بعدی",
         "TA": "هنرمند فنی / VFX", "DES": "طراح بازی / بالانس / تولید", "AUD": "صدا (پیمانکار)", "QA": "تست و کیفیت"}
HEAD = {"PROG": 2, "NETP": 1, "UIP": 1, "ART": 2, "TA": 1, "DES": 1, "AUD": 0.5, "QA": 0.5}
MONTHS = {"P0": 1.9, "MVP": 8.0, "P2": 7.3, "P3": 6.6}
PH = ["P0", "MVP", "P2", "P3"]
PH_NAME = {"P0": "فاز ۰", "MVP": "MVP", "P2": "فاز ۲ ← انتشار ۱.۰ (۶ طبقه)", "P3": "آپدیت بزرگ ۱ (طبقات ۷–۱۰)"}

tot = collections.defaultdict(float)
for r in rows:
    tot[(r["Phase"], r["Role"])] += float(r["Estimate_pd"])
touched = [r for r in rows if r["Status"] != "Todo"]

L = ["# بک‌لاگ کامل تسک‌ها — برج آشوب / Bonkspire", "",
     f"**منبع حقیقت:** `TASKS.csv` (UTF-8؛ قابل ایمپورت به Jira / Linear / Notion / Excel). این فایل از روی آن ساخته می‌شود: `python -X utf8 Core/tools/tasks_csv_to_md.py`.  ",
     f"**تعداد تسک:** {len(rows)} در {len({r['Epic'] for r in rows})} اپیک. **برآورد:** روز-نفر (`pd`)، برآورد برنامه‌ریزی اولیه، نه تعهد.  ",
     f"**وضعیت:** {len(touched)} تسک لمس شده (≈ {sum(float(r['Estimate_pd']) for r in touched):.0f} از {sum(float(r['Estimate_pd']) for r in rows):.0f} روز-نفر)؛ بیشترشان «Logic done» یا «Partial» هستند، نه نهایی.", "",
     "## راهنما",
     "* **فازها:** `P0` نمونه عمودی، `MVP` بتای بسته ایران (۳ طبقه)، `P2` تکمیل تا **انتشار ۱.۰ با ۶ طبقه** (+ Shard جهانی)، `P3` = **آپدیت بزرگ ۱** (طبقات ۷ تا ۱۰). برنامه انتشار: SPEC §۲۹.",
     "* **نقش‌ها:** " + "، ".join(f"`{k}` = {v}" for k, v in ROLES.items()) + ".",
     "* **تعریف انجام‌شده:** کد/محتوا + تست خودکار + متن fa/en + بودجه حجم و عملکرد رعایت‌شده + بازبینی همتا.", "",
     "## خلاصه بار کاری در برابر ظرفیت (روز-نفر)", "",
     "ظرفیت = تعداد نفر × ماه × ۲۱ روز × ضریب تمرکز ۰.۷. نسبت > ۱.۰ در یک نقش طبیعی است؛ ملاک اصلی جدول تجمیعی انتهای فایل است.", "",
     "| نقش | تیم | " + " | ".join(f"{PH_NAME[p]} (کار / ظرفیت)" for p in PH) + " |",
     "| :--- | :--- | " + " | ".join(":---" for _ in PH) + " |"]
for r in ROLES:
    cells = []
    for p in PH:
        cap = HEAD[r] * MONTHS[p] * 21 * 0.7
        w = tot[(p, r)]
        cells.append(f"{w:.0f} / {cap:.0f} ({w / cap:.2f})")
    L.append(f"| `{r}` {ROLES[r]} | {HEAD[r]} | " + " | ".join(cells) + " |")
L.append("")

epics = collections.OrderedDict()
for r in rows:
    epics.setdefault(r["Epic"], []).append(r)
for e, rs in epics.items():
    L.append(f"## {e}  ({len(rs)} تسک، {sum(float(x['Estimate_pd']) for x in rs):.0f} pd)")
    L += ["", "| ID | تسک | نقش | فاز | pd | وابستگی | معیار پذیرش | مرجع | وضعیت |", "| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |"]
    for x in rs:
        L.append(f"| {x['ID']} | {x['Title']} | {x['Role']} | {x['Phase']} | {x['Estimate_pd']} | {x['Depends_on'].replace(';', '، ')} | {x['Acceptance']} | {x['Spec_ref']} | {x['Status']} |")
    L.append("")
L.append(open(TAIL, encoding="utf-8").read().strip())
open(OUT, "w", encoding="utf-8").write("\n".join(L) + "\n")
print("wrote", OUT, len(rows), "tasks")
