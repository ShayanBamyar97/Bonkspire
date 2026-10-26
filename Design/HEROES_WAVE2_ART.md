# موج دوم هیروها: جهت هنری و برگه کانسپت

**همراه:** [`HEROES_WAVE2.md`](HEROES_WAVE2.md) (کیت و اسکین‌ها) · **مرجع‌ها:** SPEC §۲۶ (کتاب هنر)، §۲۳.۳ (بودجه)، §۲۰ (بدون متن روی مدل)، §۲۶.۴ (زبان رنگ تله‌ها)، §۲۶.۷ (سطوح کیفیت)  
**تاریخ:** ۲۰۲۶-۱۰-۲۶ · **وضعیت:** پیشنهاد

این سند از ۱۰ تصویر مرجعی که صاحب پروژه فرستاد **اصول** را بیرون می‌کشد و روی ۱۰ هیرو پیاده می‌کند. خود تصاویر وارد مخزن نمی‌شوند.

---

## ۰. قانون استفاده از مرجع‌ها (حقوقی)

چند مرجع، شخصیت‌های شناخته‌شده کمیک، فیلم یا بازی‌اند. **فقط اصل طراحی را برمی‌داریم، نه ترکیب امضای آن شخصیت را.**

| ممنوع (ترکیب امضای یک شخصیت دیگر) | مجاز (اصل قابل‌استفاده) |
| :--- | :--- |
| کوله سوسکی + پاهای عنکبوتی از پشت | زائده‌های پشتی برای سیلوئت از بالا |
| ماسک با عدسی گرد سفید + کلاه شاپو + بارانی بلند، با هم | کلاه لبه‌پهن یا بارانی، **جدا** و در ترکیب خودمان |
| لاک‌پشت + نوار چشم رنگی + دو خنجر کوتاه | تناسب چاق و پاهای پهن |
| الگوی دقیق لباس قهرمان‌های کمیک | رنگ‌بندی بلوکی ۳ رنگ |
| نوشته «POLICE» یا هر کلمه روی لباس | نماد بدون متن (SPEC §۲۰: هیچ متنی روی مدل) |

* تصاویر مرجع **در مخزن commit نمی‌شوند** و **به‌عنوان تصویر ورودی به مدل‌های تولید تصویر داده نمی‌شوند.** پرامپت‌های بخش ۶ فقط متنی‌اند.

---

## ۱. از هر مرجع چه برمی‌داریم

| # | مرجع (توصیف) | اصل طراحی | کجا استفاده می‌شود |
| :--- | :--- | :--- | :--- |
| ۱ | تندیس قهرمان با لباس سه‌رنگ بلوکی | **بلوک‌های بزرگ رنگ** که از دور خوانا‌اند؛ هر بلوک = یک کانال ماسک برای اسکین Rare | قانون رنگ کل موج؛ گردآفرید، اسفندیار |
| ۲ | زره حشره‌ای-فناوری با خطوط درخشان و زائده‌های پشتی | **خطوط تأکید درخشان** که می‌توانند کارکرد داشته باشند (پر شدن با انرژی التیمیت)؛ زائده پشتی = سیلوئت از بالا | سیما، ضحاک Epic، سیما Mythic |
| ۳ | کارآگاه نوآر با بارانی، کلاه و عینک گرد | **لبه کلاه از بالا بزرگ‌ترین شکل است**؛ پالت تیره بدون رنگ تأکید روی زمین تیره گم می‌شود | آرش، بختک، زروان Epic |
| ۴ | برگه کانسپت آبرنگی با زره چاق، پالت و حالت چهره | **قالب برگه کانسپت** (بخش ۵)؛ نسخه بی‌کلاه‌خود = ایده اسکین | قالب همه هیروها؛ گردآفرید |
| ۵ | شوالیه طلایی با بال روی کلاه‌خود و سرشانه | **بال و تاج روی سر و شانه** سیلوئت می‌سازد؛ شنل سفید = حرکت ثانویه | اسفندیار Mythic، زروان Mythic |
| ۶ | موجود بیگانه با زره مسی و پاهای وارونه (`Digitigrade`) | **آناتومی غیرانسانی** (زانوی معکوس، خار سر) برای هیروهای موجودی | ضحاک Mythic، بختک Mythic؛ همچنین هیولاهای §۵ |
| ۷ | شبکه ۹ شخصیت استایلایز (انسان، موجود، ربات) | **یکپارچگی گروه:** یک نورپردازی و یک زبان متریال برای همه، ولی هرکس یک شکل غالب و یک داستان رنگ | کل موج؛ موجود سنگین ← بختک؛ سبیل و بند شلوار ← زروان Epic |
| ۸ | پلیس آینده با پهپادهای همراه و نقاب نقطه‌دار سبز | **پهپاد همراه در ارتفاع سر** کاملاً خواناست؛ تک‌رنگ + یک درخشش | سیما، گردآفرید Mythic |
| ۹ | تندیس جنگجوی لاک‌پشتی چاق | **تناسب چاق**، ایستادن پهن، دست و پای بزرگ، نوار پارچه برای حرکت ثانویه | دیو سپید، ضحاک، فرهاد |
| ۱۰ | چهار پرتره نیروی ویژه با درخشش نقاب متفاوت | **رنگ درخشش نقاب = هویت فوری**؛ چهار نسخه از یک بدن با سرپوش متفاوت = همان منطق سطح‌بندی اسکین | اسکین‌های گردآفرید؛ قانون Epic (بخش ۳) |

---

## ۲. ترجمه برای دوربین بالا (`54°`) — مهم‌ترین قانون این سند

بیشتر مرجع‌ها از روبه‌رو و از نزدیک دیده می‌شوند؛ بازی ما **از بالا** و از دور است.

* **اندازه روی صفحه:** با اعداد دوربین SPEC (§۲.۳، عرض دید ≈ ۱۹.۵ متر) هیرو روی گوشی تقریباً **۸۰ تا ۱۰۰ پیکسل** ارتفاع دارد. این عدد تخمینی است و باید در Unity اندازه‌گیری شود.
* **سقف را طراحی کنید:** دوربین بیشتر **سر، شانه‌ها، پشت و کلاه** را می‌بیند. هویت هیرو باید آن‌جا باشد: لبه کلاه، شاخ، مار روی شانه، بال پهپادی، دیسک سپر.
* **چه می‌میرد:** نشان روی سینه، حکاکی، نقش ریز پارچه، حالت صورت، خطوط باریک. این‌ها فقط برای **ویترین ۳بعدی فروشگاه، لابی و `Cut-in` التیمیت** هستند.
* **۳ بلوک رنگ + ۱ تأکید** (SPEC §۲۶.۲). هر جزئیاتی که روی صفحه کوچک‌تر از چند پیکسل شود، با رنگ یا شکل جایگزین می‌شود، نه با بافت.
* **درخشش بدون Bloom:** کیفیت `Low` هیچ `Bloom` ندارد (§۲۶.۷). پس درخشش = رنگ روشن `Unlit` + هاله نقاشی‌شده در بافت؛ نباید به Bloom وابسته باشد.
* **تست قبولی:** (۱) سیلوئت سیاه در اندازه بازی؛ (۲) تست «چشم تنگ‌کردن» کنار ۴ هیروی دیگر؛ (۳) دیده شدن روی تیره‌ترین طبقه (۳ تیمارستان) و روشن‌ترین (۸ یخبندان).

## ۳. قوانین یکپارچگی گروه

1. **یک زبان رندر:** کانتور ۲ پیکسل + `Rim Light` سفارشی + بافت گرادیانی ۱۰۲۴² برای همه (§۲۶.۲). مرجع‌های واقع‌گرا (۲، ۳، ۶، ۸، ۱۰) به همین زبان ترجمه می‌شوند: بدون نقشه نرمال برای جزئیات کوچک؛ متریال با رنگ و ارزش نوری نشان داده می‌شود.
2. **رنگ زمین مال تله‌هاست:** حلقه لبه هر مهارتی که روی زمین اثر می‌گذارد از زبان رنگ تله‌ها پیروی می‌کند (قرمز = دمیج، زرد/نارنجی = کنترل، بنفش = اختلال حسی، سبز = امن/پاداش، آبی = اطلاعات). رنگ تم هیرو فقط داخل ناحیه است.
3. **حلقه بازیکن غالب می‌ماند:** دامن، شنل یا ردایی که روی زمین می‌کشد، رنگ اشباع ندارد تا با حلقه رنگی بازیکن (۵ رنگ) رقابت نکند.
4. **هیروهای تیره** (بختک، اسکین‌های نوآر) Rim قوی‌تر و یک رنگ تأکید روشن روی سر دارند.
5. **درخشش با کارکرد (از مرجع ۲):** خطوط درخشان هر هیرو با انرژی التیمیت (`0 → 100`، §۱۶.۳) پر می‌شوند؛ وقتی التیمیت آماده است، کامل روشن‌اند. اطلاعات بازی بدون HUD.
6. **سطح‌بندی اسکین برای دوربین بالا:**
   * **Rare:** فقط ماسک رنگ (هر بلوک رنگ یک کانال) + لوازم کوچک ← ارزان و داخل بسته پایه.
   * **Epic:** ۱ تا ۳ قطعه مدل عوض می‌شود و **حداقل یکی از آن‌ها روی سقف است** (سرپوش، پشت یا سلاح بالای سر)؛ وگرنه بازیکن چیزی را که خریده در بازی نمی‌بیند (درس مرجع ۱۰).
   * **Mythic:** سیلوئت جدید + درخشش جدید + VFX جدید.
7. **پارچه و شنل:** سقف ۶۰ استخوان (§۲۳.۳). شنل‌ها با ۲–۴ استخوان یا تاب‌خوردن در شیدر رأس (`Vertex Shader`)، نه شبیه‌سازی پارچه.

---

## ۴. بریف بصری هر هیرو

> رنگ‌ها: ۳ رنگ اصلی + **تأکید** (کد هگز برای هماهنگی بین هنرمندان؛ مقدار نهایی در کانسپت تنظیم می‌شود).

| هیرو | پالت (۳ + تأکید) | هویت از بالا | مرجع‌ها | درخشش (کارکرد) | پرهیز |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **فرهاد** | `#C8924A` اُخرا، `#B0643A` مس، `#1F5E63` فیروزه‌ای تیره، **`#FFB23E` کهربا** | شکل «T»: کلنگ-مته افقی روی پشت + فانوس کلاه | ۹ (تناسب چاق)، ۷ | فانوس کلاه با انرژی التیمیت روشن‌تر می‌شود | حکاکی ریز سنگ‌نگاره روی بدن (از بالا دیده نمی‌شود؛ فقط در Mythic و به‌صورت بلوک‌های بزرگ) |
| **لُعبت** | `#C0283C` قرمز پرده، `#E3B23C` خردلی، `#6B3FA0` بنفش، **`#FFD86B` نخ طلایی** | دو لکه (او و عروسک) + صلیب نخ بالای سرش | ۷ (تنوع بدن)، ۴ | نخ‌های طلایی بین دست و عروسک | عروسک خیلی کوچک (حداقل نصف قد لعبت تا از بالا دیده شود) |
| **بختک** | `#1A1424` سیاه جوهری، `#5B2C6F` بنفش کبود، `#E6E1D3` سفید مهتابی، **`#FF3B3B` چشم** | قطره تیره بزرگ + نقاب سفید رو به بالا + یک نقطه قرمز | ۳ (پالت تیره)، ۷ (موجود سنگین)، ۶ | چشم با انرژی التیمیت درشت‌تر می‌شود | قرمز روی زمین (معنای دمیج دارد)؛ دامن کفن تیره و کم‌اشباع بماند |
| **زروان** | `#23264F` نیلی، `#C9A44C` برنج، `#E8D5B0` شنی، **`#3FE0D0` فیروزه‌ای زمان** | **حلقه‌های اسطرلاب افقی** دور بدن (از بالا عالی‌اند) + ساعت شنی | ۵ (تاج/بال برای Mythic)، ۳ | شن داخل ساعت شنی = نوار انرژی التیمیت | نقش دقیق صورت‌های فلکی (فقط Mythic، درشت) |
| **سیما** | `#12C2C2` فیروزه‌ای برقی، `#E0389C` سرخابی، `#2B2D33` گرافیتی، **`#F2C14E` پر طلایی** | «پروانه»: بال‌های پهپادی پهن افقی | ۲ (زائده پشتی، خطوط درخشان)، ۸ (پهپاد همراه) | ۳۰ پهپاد یکی‌یکی با انرژی روشن می‌شوند | شکل سوسکی برای کوله؛ پهپادها پرنده‌شکل‌اند، نه حشره‌شکل |
| **ضحاک** | `#2E8B57` یشمی، `#A8D82C` سمی، `#D4A52A` طلایی، **`#E5383B` چشم مارها** | «V» بزرگ: دو مار بلند از شانه‌ها، سرشان بالاتر از سر ضحاک | ۹ (بالاتنه سنگین)، ۲ و ۶ (Epic و Mythic) | چشم مارها؛ زهر سبز روی دندان‌ها | حالت ترسناک یا خونین (رده ۱۲+)؛ مارها بامزه و بدجنس |
| **آرش** | `#B5532E` زنگار، `#9E2235` قرمز گلیم، `#3B5B8C` آبی جین، **`#D9A441` برنج** | **دایره لبه کلاه** + خط مورب کمان | ۳ (کلاه از بالا) | نقش گلیم پانچو با انرژی روشن می‌شود | ترکیب کلاه شاپو + بارانی + ماسک عدسی‌دار (امضای مرجع ۳)؛ آرش پانچو و کلاه لبه‌پهن پردار دارد |
| **دیو سپید** | `#F2F5F7` برفی، `#8EC9E8` آبی یخچالی، `#5E6A75` لوح، **`#FF8C2E` رون** | دایره بزرگ + دو شاخ قلاب | ۹ (چاق)، ۷ | رون‌های دستبند با انرژی روشن می‌شوند | سفید خالص روی طبقه ۸ گم می‌شود ← سایه آبی و کانتور قوی |
| **گردآفرید** | `#5B6236` زیتونی، `#C2A878` خاکی، `#7D858C` فولادی، **`#C1121F` شال** | خط مورب نیزه + گیس بافته روی پشت + عینک دید در شب روی کلاه | ۱۰ (نقاب = هویت)، ۸، ۴ (نسخه بی‌کلاه) | لنزهای دید در شب = انرژی التیمیت | متن روی جلیقه؛ قرمز شال فقط روی گردن، نه روی زمین |
| **اسفندیار** | `#A86B32` مفرغ، `#1E1E24` مرمر سیاه، `#1F3C88` آبی شاهانه، **`#FFF1C1` سفید-طلایی** | **دیسک سپر** + کاکل کلاه‌خود؛ **درخشش چشم جهت نگاه را نشان می‌دهد** (مکانیک جلو/پشت) | ۵ (بال و تاج)، ۱ (بلوک رنگ) | ترک‌های نورانی بدن با «خوان»های التیمیت پر می‌شوند | بال واقعی روی پشت در طرح پایه (برای Mythic بماند) |

---

## ۵. قالب برگه کانسپت (برگرفته از مرجع ۴)

هر هیرو و هر اسکین Epic/Mythic یک برگه با این اجزا دارد:

1. **سه نما:** روبه‌رو، سه‌رخ، پشت؛ تمام‌قد، ژست خنثی، پس‌زمینه خاکستری روشن.
2. **نوار پالت:** ۴ مربع (۳ + تأکید) با کد هگز.
3. **۳ حالت چهره** برای `Cut-in` و لابی (معمولی، پیروزی، شکست یا شوخی).
4. **تصویر کوچک بازی:** همان هیرو از زاویه `54°` در اندازه واقعی روی صفحه (≈ ۸۰–۱۰۰ پیکسل)، روی کف طبقه ۳ و طبقه ۸.
5. **سیلوئت سیاه** در همان اندازه.
6. **جزئیات سلاح یا لوازم** + یک نسخه «بی‌کلاه‌خود» یا «بدون سلاح» اگر به اسکین مربوط است.
7. **یادداشت فنی:** تعداد مثلث تخمینی، استخوان‌های اضافی (مار، شنل، عروسک)، کانال‌های ماسک رنگ.

---

## ۶. پرامپت برگه کانسپت برای مدل‌های تولید تصویر

برای ایده‌پردازی سریع (مثلاً قبل از سفارش کانسپت به هنرمند). انگلیسی‌اند چون مدل‌های تصویر با انگلیسی دقیق‌ترند. اندازه پیشنهادی `16:9`، کیفیت متوسط برای پیش‌نویس و چند نسخه (`n=4`). خروجی این مدل‌ها **ایده** است، نه دارایی نهایی؛ کانسپت نهایی را هنرمند می‌کشد.

**پیشوند سبک** (اول همه پرامپت‌ها):

```
A stylized 3D character turnaround sheet for an original mobile action-game hero, rendered like a polished hand-painted game model: chunky proportions about three and a half heads tall, oversized head and hands, smooth gradient textures, a thin dark outline and a soft white rim light.
```

**پسوند ثابت** (آخر همه پرامپت‌ها):

```
Show the same character three times, front view, three-quarter view and back view, standing in a relaxed neutral pose, full body, evenly spaced on a plain light-gray background. Below the figures, a row of four flat color swatches. Soft even studio light from the front-left with a cool rim light from behind. Original design. No text, no letters, no logos, no watermark. 16:9.
```

| هیرو | بدنه پرامپت (بین پیشوند و پسوند) |
| :--- | :--- |
| **فرهاد** | `The hero is a hunched, heavily muscled stone-carver inspired by ancient Persian rock reliefs and dieselpunk mining. He wears a sandstone-ochre work tunic, copper shoulder plates and a deep-teal scarf that trails behind him, a miner's helmet with a glowing amber lantern, and carries a huge pickaxe-drill across his back so his silhouette forms a bold T shape from above. Swatches: sandstone ochre, copper, deep teal, glowing amber.` |
| **لُعبت** | `The hero is a tall, playful puppeteer girl on wooden stilts, inspired by traditional Persian marionette theater and a circus ringmaster. She wears a crimson theater-curtain coat with mustard-yellow trim and violet stockings, holds a wooden cross-bar control above her head, and golden strings run to her companion puppet: a chubby cloth strongman with a yarn mustache who stands half her height beside her and appears in every view. Swatches: curtain crimson, mustard yellow, violet, golden thread.` |
| **بختک** | `The hero is a floating nightmare spirit from Persian folklore: a heavy, drooping ink-black velvet shroud shaped like an upside-down teardrop, two long thin arms, and a cracked porcelain mask tilted slightly upward with a single glowing red eye. Bruise-purple folds and pale moon-white highlights, eerie but cartoon-friendly, suitable for a 12+ game. Swatches: ink black, bruise purple, moon white, glowing red.` |
| **زروان** | `The hero is a tall, ageless robed time-keeper with a lion-mane hood that hides the face, inspired by Persian astrolabes and surreal melting clocks. Three thin brass astrolabe rings orbit horizontally around the body, and a large hourglass filled with glowing cyan sand rides on the back. Midnight-indigo robe with brass trim and sand-beige sashes. Swatches: midnight indigo, brass, sand beige, glowing cyan.` |
| **سیما** | `The hero is a slim teenage hacker girl in an oversized graphite hoodie and a neon visor, whose wide wings are made of thirty small bird-shaped drones spread horizontally like a butterfly, inspired by Persian geometric tile patterns and cyberpunk streetwear. Electric-teal and magenta light lines run along the drones and sleeves, with small golden feather accents. Swatches: electric teal, magenta, graphite, gold.` |
| **ضحاک** | `The hero is a broad, top-heavy ancient-Persian-inspired king in a mud-splashed royal robe and a gold crown, with two long cartoonish snakes rising from his shoulders to form a big V shape higher than his head. The left snake looks nervous and the right one looks lazy; both are mischievous rather than scary. Jade-green robe, toxic chartreuse venom drops, royal gold trim, red snake eyes. Swatches: jade green, chartreuse, royal gold, red.` |
| **آرش** | `The hero is a lean frontier archer blending a legendary Persian bowman with spaghetti-western style: a poncho woven like a red Persian kilim rug, denim-blue trousers, a wide-brimmed desert hat with a single feather whose brim reads as a large circle from above, and a tall composite recurve bow carried over the shoulder like a rifle. Desert-rust leather and brass buckles. Swatches: desert rust, kilim red, denim blue, brass.` |
| **دیو سپید** | `The hero is a huge, round, fluffy snow giant, a gentle and slightly clumsy creature with a small head on a massive snowball-shaped body, curled ram horns, big flat feet and heavy slate-stone bracelets carved with glowing orange runes. Snow-white fur with glacier-blue shadows. Friendly, cuddly and strong. Swatches: snow white, glacier blue, slate gray, glowing orange.` |
| **گردآفرید** | `The hero is an agile female infiltrator who blends ancient Sasanian lamellar armor with modern tactical gear: an olive-drab plate carrier covered in scale-like lamellar plates, a desert-tan helmet with flip-up night-vision lenses, a long braid falling down her back, a crimson scarf, and a hybrid lance-carbine held diagonally. Steel-gray buckles and pouches, no lettering anywhere. Swatches: olive drab, desert tan, steel gray, crimson.` |
| **اسفندیار** | `The hero is a statuesque bronze champion who looks like a living ancient statue: a crested helmet, a large round bronze shield with a sun-disc motif, thin cracks across the bronze body glowing with white-gold light, black-marble armor plates with royal-blue cloth. His glowing eyes clearly show which way he faces. Swatches: bronze, black marble, royal blue, white-gold glow.` |

**پرامپت اسکین (ویرایش با فهرست ثابت‌ها):** وقتی برگه پایه تأیید شد، آن را به‌عنوان تصویر مرجع بدهید و فقط تغییر را بگویید:

```
Using the approved character sheet as the reference, change only: [the skin's new palette / accessory / replaced parts, e.g. "the miner's helmet becomes a space helmet and the pickaxe becomes a laser drill"]. Keep unchanged: body proportions, silhouette size, pose, the three views and their spacing, outline and rim-light style, background and swatch layout. No text, no logos, no watermark. 16:9.
```

---

## ۷. ترتیب کار پیشنهادی برای هر هیرو

1. پرامپت بخش ۶ ← ۸ تا ۱۲ ایده سریع.
2. انتخاب جهت ← هنرمند برگه کانسپت کامل (بخش ۵) را می‌کشد.
3. **تست دوربین بالا** با یک مدل خاکستری ساده در Unity (سیلوئت + تأکید) پیش از مدل‌سازی کامل.
4. مدل، ریگ، انیمیشن (`E14`) ← اسکین Rare (ماسک) ← Epic ← Mythic.
