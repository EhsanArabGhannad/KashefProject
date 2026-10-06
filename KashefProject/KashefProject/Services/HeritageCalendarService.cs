using System.Globalization;
using System.Text;
using KashefProject.Data;
using KashefProject.Models;

namespace KashefProject.Services;

public sealed class HeritageCalendarService
{
    public const int MinYear = 2480;
    public const int MaxYear = 2680;

    public HeritageCalendarViewModel Build(int? year, int? month, int? day, string? category,
        string? query, string? convention, DateTimeOffset? now = null, string? language = null)
    {
        var pc = new PersianCalendar();
        var iranTime = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"));
        var today = DateOnly.FromDateTime(iranTime.DateTime);
        var imperialYear = year ?? HeritageCalendarText.ImperialYear(pc.GetYear(iranTime.DateTime));
        var m = month ?? pc.GetMonth(iranTime.DateTime);
        if (imperialYear is < MinYear or > MaxYear || m is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(year));
        var y = HeritageCalendarText.SolarHijriYear(imperialYear);
        var daysInMonth = pc.GetDaysInMonth(y, m);
        var d = day ?? (y == pc.GetYear(iranTime.DateTime) && m == pc.GetMonth(iranTime.DateTime) ? pc.GetDayOfMonth(iranTime.DateTime) : 1);
        if (d < 1 || d > daysInMonth) throw new ArgumentOutOfRangeException(nameof(day));
        category = category is "ancient" or "royal" or "memorial" ? category : "all";
        convention = convention == "seasonal" ? "seasonal" : "named";
        language = language == "fa" ? "fa" : "en";
        query = (query ?? "").Trim();
        if (query.Length > 80) query = query[..80];
        var search = Normalize(query);
        var definitions = HeritageCalendarCatalog.Events.Where(e =>
            (category == "all" || e.Category == category) &&
            (search.Length == 0 || Normalize(e.Title + " " + e.Description + " " + HeritageCalendarEnglish.Translate(e).Title + " " + HeritageCalendarEnglish.Translate(e).Description).Contains(search, StringComparison.Ordinal))).ToArray();
        var occurrences = Occurrences(y, convention, definitions, language);
        var start = SolarDate(y, m, 1);
        var end = start.AddDays(daysInMonth - 1);
        var selected = start.AddDays(d - 1);
        var currentYear = pc.GetYear(iranTime.DateTime);
        var upcoming = Occurrences(currentYear, convention, definitions, language)
            .Concat(Occurrences(currentYear + 1, convention, definitions, language))
            .Where(e => e.EndDate >= today).OrderBy(e => e.Date < today ? today : e.Date).ThenBy(e => e.Event.Id).Take(4).ToArray();
        return new HeritageCalendarViewModel
        {
            Year = imperialYear, Month = m, Day = d, Today = today, SelectedDate = selected,
            Category = category, Query = query, Convention = convention, Language = language,
            LeadingDays = ((int)start.DayOfWeek + 1) % 7,
            TotalEvents = HeritageCalendarCatalog.Events.Count,
            YearEvents = occurrences,
            MonthEvents = occurrences.Where(e => e.Date <= end && e.EndDate >= start).ToArray(),
            DayEvents = occurrences.Where(e => e.Date <= selected && e.EndDate >= selected).ToArray(),
            Upcoming = upcoming,
            Days = Enumerable.Range(1, daysInMonth).Select(number =>
            {
                var date = start.AddDays(number - 1);
                return new HeritageCalendarDay(number, date, date == today, date == selected,
                    occurrences.Where(e => e.Date <= date && e.EndDate >= date).ToArray());
            }).ToArray()
        };
    }

    // Internal calculations use .NET PersianCalendar's Solar Hijri year; public Build accepts imperial years.
    public static DateOnly SolarDate(int year, int month, int day) =>
        DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0));

    public IReadOnlyList<HeritageOccurrence> Occurrences(int year, string convention,
        IEnumerable<HeritageEvent>? definitions = null, string language = "fa")
    {
        var start = SolarDate(year, 1, 1);
        var end = SolarDate(year + 1, 1, 1).AddDays(-1);
        var result = new List<HeritageOccurrence>();
        var fa = language == "fa";
        foreach (var definition in definitions ?? HeritageCalendarCatalog.Events)
        {
            var e = fa ? definition : HeritageCalendarEnglish.Translate(definition);
            var basis = fa ? "تاریخ ثابت شاهنشاهی (خورشیدی)" : "Fixed Imperial (solar) date";
            DateOnly date;
            switch (e.Rule)
            {
                case HeritageDateRule.Gregorian:
                    // A Persian year overlaps two Gregorian years; never use a fixed month offset.
                    for (var gy = start.Year; gy <= end.Year; gy++)
                    {
                        if (e.HistoricalYear.HasValue && gy < e.HistoricalYear.Value) continue;
                        var anniversary = new DateOnly(gy, e.Month, e.Day);
                        if (anniversary >= start && anniversary <= end)
                            result.Add(new(e, anniversary, anniversary.AddDays(e.Duration - 1), fa ? "سالگرد بر مبنای تاریخ میلادی منبع؛ معادل شاهنشاهی هر سال محاسبه می‌شود" : "Anniversary based on the source’s Gregorian date; its Imperial equivalent is recalculated each year", language));
                    }
                    continue;
                case HeritageDateRule.NameDay:
                    date = convention == "seasonal"
                        ? start.AddDays((e.Month - 1) * 30 + e.Day - 1)
                        : SolarDate(year, e.Month, e.Day);
                    basis = fa ? $"نام‌روز سنتی: {HeritageCalendarText.Number(e.Day)} {HeritageCalendarText.Months[e.Month - 1]}؛ " +
                        (convention == "seasonal" ? "تطبیق ماه‌های ۳۰روزه با مبدأ نوروز این سال" : "نمایش هم‌نام روی ماه‌های خورشیدی، نه تبدیل تقویم مذهبی")
                        : $"Traditional name-day: {e.Day} {HeritageCalendarText.MonthsFor(language)[e.Month - 1]}; " +
                        (convention == "seasonal" ? "30-day months counted from this year’s Nowruz" : "matching month/day labels, not a religious-calendar conversion");
                    break;
                case HeritageDateRule.LastWednesdayEve:
                    // The celebration is on the Tuesday BEFORE the last Wednesday, not simply the last Tuesday.
                    var lastWednesday = end;
                    while (lastWednesday.DayOfWeek != DayOfWeek.Wednesday) lastWednesday = lastWednesday.AddDays(-1);
                    date = lastWednesday.AddDays(-1);
                    basis = fa ? "شامگاه سه‌شنبه پیش از آخرین چهارشنبه سال؛ تاریخ متغیر" : "Tuesday evening before the last Wednesday of the year; variable date";
                    break;
                case HeritageDateRule.LastFiveDays:
                    date = end.AddDays(-4);
                    basis = fa ? "پنج روز پایانی سال خورشیدی؛ نمایش فصلی، با احتساب کبیسه" : "Final five days of the solar year; seasonal display including leap years";
                    break;
                default:
                    date = SolarDate(year, e.Month, e.Day);
                    break;
            }
            if (!e.HistoricalYear.HasValue || date.Year >= e.HistoricalYear.Value)
                result.Add(new(e, date, date.AddDays(e.Duration - 1), basis, language));
        }
        return result.OrderBy(e => e.Date).ThenBy(e => e.Event.Id).ToArray();
    }

    private static string Normalize(string text) => text.Normalize(NormalizationForm.FormKC)
        .Replace('ي', 'ی').Replace('ك', 'ک').Replace('\u200c', ' ').Trim().ToLowerInvariant();
}
