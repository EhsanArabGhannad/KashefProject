using System.Globalization;

namespace KashefProject.Models;

public enum HeritageDateRule { Solar, Gregorian, NameDay, LastWednesdayEve, LastFiveDays }

public sealed record HeritageEvent(
    string Id, string Title, string Category, string Description,
    HeritageDateRule Rule, int Month, int Day, string SourceTitle, string SourceUrl,
    string Note = "", int Duration = 1, int? HistoricalYear = null);

public sealed record HeritageOccurrence(HeritageEvent Event, DateOnly Date, DateOnly EndDate, string DateBasis, string Language = "fa")
{
    public string ImperialDate => HeritageCalendarText.Date(Date, Language);
    public string GregorianDate => Date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
    public string CategoryName => HeritageCalendarText.Category(Event.Category, Language);
}

public sealed record HeritageCalendarDay(int Number, DateOnly Date, bool IsToday, bool IsSelected,
    IReadOnlyList<HeritageOccurrence> Events);

public sealed class HeritageCalendarViewModel
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Day { get; init; }
    public string Category { get; init; } = "all";
    public string Query { get; init; } = "";
    public string Convention { get; init; } = "named";
    public string Language { get; init; } = "en";
    public bool IsPersian => Language == "fa";
    public DateOnly Today { get; init; }
    public DateOnly SelectedDate { get; init; }
    public int LeadingDays { get; init; }
    public int TotalEvents { get; init; }
    public IReadOnlyList<HeritageCalendarDay> Days { get; init; } = [];
    public IReadOnlyList<HeritageOccurrence> MonthEvents { get; init; } = [];
    public IReadOnlyList<HeritageOccurrence> DayEvents { get; init; } = [];
    public IReadOnlyList<HeritageOccurrence> YearEvents { get; init; } = [];
    public IReadOnlyList<HeritageOccurrence> Upcoming { get; init; } = [];
    public string MonthName => HeritageCalendarText.MonthsFor(Language)[Month - 1];
    public int PreviousYear => Month == 1 ? Year - 1 : Year;
    public int PreviousMonth => Month == 1 ? 12 : Month - 1;
    public int NextYear => Month == 12 ? Year + 1 : Year;
    public int NextMonth => Month == 12 ? 1 : Month + 1;
}

public static class HeritageCalendarText
{
    // The imperial era changes the year number, not the solar months or leap-year rules.
    public const int ImperialYearOffset = 1180;
    public static int ImperialYear(int solarHijriYear) => checked(solarHijriYear + ImperialYearOffset);
    public static int SolarHijriYear(int imperialYear) => checked(imperialYear - ImperialYearOffset);
    public static readonly string[] Months = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];
    public static readonly string[] Weekdays = ["شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه"];
    private static readonly string[] EnglishMonths = ["Farvardin", "Ordibehesht", "Khordad", "Tir", "Mordad", "Shahrivar", "Mehr", "Aban", "Azar", "Dey", "Bahman", "Esfand"];
    private static readonly string[] EnglishWeekdays = ["Sat", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri"];
    public static string[] MonthsFor(string language) => language == "fa" ? Months : EnglishMonths;
    public static string[] WeekdaysFor(string language) => language == "fa" ? Weekdays : EnglishWeekdays;
    public static string Number(int value, string language = "fa") => language == "fa"
        ? string.Concat(value.ToString(CultureInfo.InvariantCulture).Select(c => c is >= '0' and <= '9' ? (char)('۰' + c - '0') : c))
        : value.ToString(CultureInfo.InvariantCulture);
    public static string Date(DateOnly date, string language = "fa")
    {
        var calendar = new PersianCalendar();
        var value = date.ToDateTime(TimeOnly.MinValue);
        return $"{Number(calendar.GetDayOfMonth(value), language)} {MonthsFor(language)[calendar.GetMonth(value) - 1]} {Number(ImperialYear(calendar.GetYear(value)), language)}";
    }
    public static string Category(string value, string language = "fa") => language == "fa" ? value switch
    {
        "ancient" => "جشن‌ها و آیین‌ها",
        "royal" => "شاهنشاهی",
        "memorial" => "یادبودهای معاصر",
        _ => "همه رویدادها"
    } : value switch { "ancient" => "Festivals & traditions", "royal" => "Royal anniversaries", "memorial" => "Modern commemorations", _ => "All events" };
}
