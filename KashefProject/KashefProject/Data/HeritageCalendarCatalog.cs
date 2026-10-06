using KashefProject.Models;

namespace KashefProject.Data;

// Editorial, source-backed catalogue. Do not invent precise anniversaries for ancient events
// whose day/month is unknown. New entries need a source and an explicit recurrence rule.
public static class HeritageCalendarCatalog
{
    private const string NameDays = "https://avesta.org/zcal.html";
    private const string Modi = "https://www.avesta.org/ritual/rcc1937.pdf";
    private const string Iranica = "https://www.iranicaonline.org/articles/";
    private const string NamesNote = "این جشن نام‌روز است؛ تاریخ برگزاری در تقویم‌های زرتشتی و میان جوامع مختلف یکسان نیست. مبنای نمایش را از تنظیمات تقویم انتخاب کنید.";

    public static IReadOnlyList<HeritageEvent> Events { get; } = Array.AsReadOnly<HeritageEvent>(
    [
        new("nowruz", "نوروز", "ancient", "آغاز سال خورشیدی و جشن نوزایی طبیعت؛ از آیین‌های ماندگار فرهنگ ایرانی.",
            HeritageDateRule.Solar, 1, 1, "ایرانیکا؛ نوروز در تقویم ایرانی", Iranica + "nowruz-iii/", "این تقویم روز نوروز را نشان می‌دهد، نه ساعت دقیق تحویل سال."),
        new("great-nowruz", "نوروز بزرگ؛ روز امید", "ancient", "ششم فروردین در منابع تاریخی به نوروز بزرگ و روز امید شناخته می‌شود.",
            HeritageDateRule.Solar, 1, 6, "ایرانیکا؛ نوروز در تقویم ایرانی", Iranica + "nowruz-iii/"),
        new("zoroaster-birthday", "زادروز سنتی زرتشت", "ancient", "یادبود زادروز زرتشت در سنت زرتشتی؛ تاریخ واقعی تولد او معلوم نیست.",
            HeritageDateRule.NameDay, 1, 6, "ایرانیکا؛ آیین‌های تبریک", Iranica + "congratulations-pers/", "ششم فروردین، تاریخ آیینی است و نباید به‌عنوان تاریخ اثبات‌شده تولد تاریخی تلقی شود."),
        new("sizdah-bedar", "سیزده‌به‌در", "ancient", "گردهمایی در طبیعت در پایان جشن‌های نوروزی؛ خاستگاه دقیق این آیین محل بحث است.",
            HeritageDateRule.Solar, 1, 13, "ایرانیکا؛ نوروز در تقویم ایرانی", Iranica + "nowruz-iii/"),
        Named("farvardigan", "فروردینگان", 1, 19, "یادبود فروهرها و درگذشتگان.", Modi, "مودی؛ آیین‌های پارسیان، بخش جشن‌ها", "این جشن با فروردیگانِ پایان سال متفاوت است."),
        Named("ordibeheshtgan", "اردیبهشتگان", 2, 3, "جشن نام‌روز اردیبهشت؛ پاسداشت راستی و آتش.", Modi, "مودی؛ آیین‌های پارسیان، بخش جشن‌ها"),
        Named("khordadgan", "خردادگان", 3, 6, "جشن نام‌روز خرداد؛ پیوند با آب و کمال.", Modi, "مودی؛ آیین‌های پارسیان، بخش جشن‌ها"),
        Named("tirgan", "تیرگان", 4, 13, "جشن تیر و آب؛ پیوند با روایت آرش کمانگیر.", Iranica + "hafta-week-history-of-the-weeky-calendar-in-iran-1/", "ایرانیکا؛ هفته و تیرگان", "روایت آرش، روایتی اسطوره‌ای است، نه رویدادی با تاریخ تاریخیِ قطعی."),
        Named("amordadgan", "امردادگان", 5, 7, "جشن نام‌روز امرداد؛ پاسداشت گیاهان و زندگی."),
        Named("shahrivargan", "شهریورگان", 6, 4, "جشن نام‌روز شهریور؛ پیوند با شهریاری آرمانی."),
        Named("mehrgan", "مهرگان", 7, 16, "جشن مهر، پیمان و دوستی؛ از جشن‌های بزرگ ایران.", Iranica + "mehragan/", "ایرانیکا؛ مهرگان"),
        Named("abangan", "آبانگان", 8, 10, "جشن نام‌روز آبان؛ پاسداشت آب‌ها.", Iranica + "aban-mah/", "ایرانیکا؛ آبان ماه"),
        Named("azargan", "آذرگان", 9, 9, "جشن نام‌روز آذر؛ پاسداشت آتش."),
        new("yalda", "شب یلدا؛ چله", "ancient", "آخرین شب پاییز؛ دورهمی، قصه و شعر، با نمادهایی از نور و گرمای زندگی.",
            HeritageDateRule.Solar, 9, 30, "یونسکو؛ یلدا / چله", "https://ich.unesco.org/en/RL/yald-chella-01877", "رویداد از شامگاه سی‌ام آذر آغاز می‌شود؛ این صفحه زمان نجومی انقلاب زمستانی را محاسبه نمی‌کند."),
        Named("deygan-1", "دیگان؛ اورمزد روز", 10, 1, "نخستین جشن دی‌ماه، در بزرگداشت آفریدگار."),
        Named("deygan-8", "دیگان؛ دی‌به‌آذر", 10, 8, "جشن دی‌به‌آذر در ماه دی."),
        Named("deygan-15", "دیگان؛ دی‌به‌مهر", 10, 15, "جشن دی‌به‌مهر در ماه دی."),
        Named("deygan-23", "دیگان؛ دی‌به‌دین", 10, 23, "جشن دی‌به‌دین در ماه دی."),
        Named("bahmangan", "بهمنگان", 11, 2, "جشن نام‌روز بهمن؛ پاسداشت اندیشه نیک.", Iranica + "bahmanjana-arabicized-form-of-mid/", "ایرانیکا؛ بهمنجنه / بهمنگان"),
        new("sadeh", "جشن سده", "ancient", "جشن زمستانی آتش و گردهمایی؛ از میراث مشترک ایران و تاجیکستان.",
            HeritageDateRule.Solar, 11, 10, "ایرانیکا؛ جشن سده", Iranica + "sada-festival/", "مبنای این صفحه دهم بهمنِ خورشیدی است؛ تاریخ میلادی با سال تغییر می‌کند."),
        Named("sepandarmazgan", "سپندارمذگان", 12, 5, "جشن سپندارمذ؛ پاسداشت زمین و زنان.", Iranica + "kashan-vi-the-esbandi-festival/", "ایرانیکا؛ جشن اسفندی"),
        new("chaharshanbe-suri", "چهارشنبه‌سوری", "ancient", "آیین آتش در شامگاه پیش از آخرین چهارشنبه سال؛ همراه با رسم‌های گوناگون محلی.",
            HeritageDateRule.LastWednesdayEve, 12, 1, "ایرانیکا؛ چهارشنبه‌سوری", Iranica + "caharsanba-suri/", "این مناسبت تاریخ ثابت ماهانه ندارد و برای هر سال جداگانه محاسبه می‌شود."),
        Gahambar("maidyozarem", "گاهنبار میدیوزرم", 2, 11, "جشن فصلی میانه بهار."),
        Gahambar("maidyoshahem", "گاهنبار میدیوشهم", 4, 11, "جشن فصلی میانه تابستان."),
        Gahambar("paitishahem", "گاهنبار پیته‌شهم", 6, 26, "جشن فصلی برداشت محصول."),
        Gahambar("ayathrem", "گاهنبار ایاثرم", 7, 26, "جشن فصلی بازگشت گله‌ها."),
        Gahambar("maidyarem", "گاهنبار میدیارم", 10, 16, "جشن فصلی میانه زمستان."),
        new("hamaspathmaidyem", "گاهنبار همسپتمدم؛ پایان سال", "ancient", "جشن پایان سال و یادبود فروهرها.",
            HeritageDateRule.LastFiveDays, 12, 1, "ایرانیکا؛ فروردیگان", Iranica + "frawardigan/", "نمایش فصلیِ پنج روز پایانی سال؛ جایگاه روزهای افزوده در تقویم‌های مذهبی متفاوت است.", 5),
        new("reza-shah-birthday", "زادروز رضاشاه پهلوی", "royal", "سالگرد زادروز بنیان‌گذار دودمان پهلوی؛ دربار این مناسبت را در ۲۴ اسفند گرامی می‌داشت.",
            HeritageDateRule.Solar, 12, 24, "ایرانیکا؛ بار در دوره قاجار و پهلوی", Iranica + "bar-ii-the-qajar-and-pahlavi-periods/", HistoricalYear: 1878),
        new("reza-shah-coronation", "تاج‌گذاری رضاشاه", "royal", "سالگرد مراسم تاج‌گذاری رضاشاه در کاخ گلستان، در ۲۵ آوریل ۱۹۲۶.",
            HeritageDateRule.Gregorian, 4, 25, "ایرانیکا؛ ایران لیگ", Iranica + "iran-league/", HistoricalYear: 1926),
        new("mohammad-reza-accession", "آغاز پادشاهی محمدرضاشاه", "royal", "انتقال سلطنت پس از کناره‌گیری رضاشاه، در ۱۶ سپتامبر ۱۹۴۱، در جریان اشغال ایران.",
            HeritageDateRule.Gregorian, 9, 16, "ایرانیکا؛ تاریخ ایران، دوره محمدرضاشاه", Iranica + "iran-ii2-islamic-period-page-6/", HistoricalYear: 1941),
        new("farah-birthday", "زادروز شهبانو فرح پهلوی", "royal", "سالگرد تولد فرح دیبا در تهران، در ۱۴ اکتبر ۱۹۳۸.",
            HeritageDateRule.Gregorian, 10, 14, "بنیاد شهبانو فرح پهلوی؛ زندگی‌نامه", "https://www.farahpahlavifoundation.org/who", HistoricalYear: 1938),
        new("mohammad-reza-birthday", "زادروز محمدرضاشاه پهلوی", "royal", "سالگرد زادروز محمدرضاشاه؛ مناسبت درباریِ چهارم آبان.",
            HeritageDateRule.Solar, 8, 4, "ایرانیکا؛ بار در دوره قاجار و پهلوی", Iranica + "bar-ii-the-qajar-and-pahlavi-periods/", HistoricalYear: 1919),
        new("imperial-coronation", "تاج‌گذاری شاه و شهبانو", "royal", "سالگرد تاج‌گذاری محمدرضاشاه و شهبانو فرح در چهارم آبان ۲۵۲۶ شاهنشاهی (۲۶ اکتبر ۱۹۶۷)، در کاخ گلستان.",
            HeritageDateRule.Solar, 8, 4, "ایرانیکا؛ جواهرات سلطنتی ایران", Iranica + "crown-jewels-of-persia-the-assemblage-of-jewels-collected-by-the-kings-of-persia-kept-now-in-the-bank-e-markazi-e-iran-/", HistoricalYear: 1967),
        new("reza-pahlavi-birthday", "زادروز رضا پهلوی", "royal", "سالگرد تولد ولیعهد پیشین ایران، در ۳۱ اکتبر ۱۹۶۰.",
            HeritageDateRule.Gregorian, 10, 31, "وب‌سایت فرح پهلوی؛ خانواده سلطنتی", "https://farahpahlavi.org/", HistoricalYear: 1960),
        new("royal-wedding", "سالگرد ازدواج شاه و فرح دیبا", "royal", "سالگرد ازدواج محمدرضاشاه و فرح دیبا در تهران، در ۲۱ دسامبر ۱۹۵۹.",
            HeritageDateRule.Gregorian, 12, 21, "وب‌سایت فرح پهلوی؛ ازدواج سلطنتی", "https://farahpahlavi.org/", HistoricalYear: 1959),
        new("mohammad-reza-memorial", "یادبود درگذشت محمدرضاشاه", "royal", "سالگرد درگذشت محمدرضاشاه در مصر، در ۲۷ ژوئیه ۱۹۸۰.",
            HeritageDateRule.Gregorian, 7, 27, "ایرانیکا؛ بحران گروگان‌گیری", Iranica + "hostage-crisis/", HistoricalYear: 1980),
        new("cyrus-day", "روز کوروش بزرگ", "memorial", "یادبود غیررسمی و معاصر کوروش؛ با پیوند نمادین به روایت ورود او به بابل.",
            HeritageDateRule.Solar, 8, 7, "مرکز مطالعات ایران دانشگاه تل‌آویو؛ روز کوروش", "https://en-humanities.tau.ac.il/iranian/publications/irans_pulse/2017-3", "هفتم آبان یک یادبود معاصر است، نه جشن باستانی یا روز رسمی یونسکو. تاریخ‌های باستانی، قراردادهای تبدیل متفاوتی دارند.")
    ]);

    private static HeritageEvent Named(string id, string title, int month, int day, string description,
        string source = NameDays, string sourceTitle = "اوستا؛ جدول نام‌روزهای زرتشتی", string note = "") =>
        new(id, title, "ancient", description, HeritageDateRule.NameDay, month, day, sourceTitle, source,
            NamesNote + (note.Length == 0 ? "" : " " + note));

    private static HeritageEvent Gahambar(string id, string title, int month, int day, string description) =>
        new(id, title, "ancient", description, HeritageDateRule.NameDay, month, day,
            "مودی؛ آیین‌های پارسیان، گاهنبارها", Modi, NamesNote, 5);
}
