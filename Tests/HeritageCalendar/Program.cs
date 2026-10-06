using System.Globalization;
using KashefProject.Data;
using KashefProject.Models;
using KashefProject.Services;

var calendar = new HeritageCalendarService();
var pc = new PersianCalendar();
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    checks++;
}
HeritageCalendarViewModel Month(int year, int month, int? day = null, string? category = null,
    string? query = null, string? convention = null, DateTimeOffset? now = null) =>
    calendar.Build(HeritageCalendarText.ImperialYear(year), month, day, category, query, convention, now);
HeritageOccurrence Event(int year, string id, string convention = "named") =>
    calendar.Occurrences(year, convention).Single(e => e.Event.Id == id);

Check(Month(1399, 12).Days.Count == 30, "Leap Esfand has 30 days");
Check(Month(1400, 12).Days.Count == 29, "Common Esfand has 29 days");
Check(Month(1405, 1).Days.Count == 31 && Month(1405, 7).Days.Count == 30, "Solar month lengths");
Check(Event(1405, "mehrgan").Date == HeritageCalendarService.SolarDate(1405, 7, 16), "Named Mehrgan");
Check(Event(1405, "mehrgan", "seasonal").Date == HeritageCalendarService.SolarDate(1405, 7, 10), "Thirty-day Mehrgan");
Check(Event(1405, "bahmangan", "seasonal").Date == HeritageCalendarService.SolarDate(1405, 10, 26), "Thirty-day Bahmangan");
Check(Month(1405, 8, 4).DayEvents.Count(e => e.Event.Category == "royal") == 2, "Multiple same-day events");
Check(Month(1405, 1, 1, "royal").YearEvents.All(e => e.Event.Category == "royal"), "Category filter");
Check(Month(1405, 1, 1, query: "كوروش").YearEvents.Single().Event.Id == "cyrus-day", "Arabic kaf search normalization");
Check(Month(1405, 1, 1, query: "تاج گذاری").YearEvents.Count == 2, "ZWNJ search normalization");
Check(Month(1405, 1, 1, query: "no such occasion").Upcoming.Count == 0, "Empty search");
Check(Month(1405, 1).PreviousMonth == 12 && Month(1405, 1).PreviousYear == 2584, "Previous imperial year boundary");
Check(Month(1405, 12).NextMonth == 1 && Month(1405, 12).NextYear == 2586, "Next imperial year boundary");
Check(HeritageCalendarText.ImperialYear(1405) == 2585 && HeritageCalendarText.SolarHijriYear(2585) == 1405, "Imperial conversion round trip");
var current = calendar.Build(null, null, null, null, null, null, new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
Check(current.Year == 2585 && current.Month == 7 && current.Day == 13, "Current imperial year without query");
Check(HeritageCalendarText.Date(current.Today) == "۱۳ مهر ۲۵۸۵", "Imperial date formatter");
Check(calendar.Build(2585, 8, 4, null, null, null).DayEvents.All(e => e.ImperialDate == "۴ آبان ۲۵۸۵"), "Event details use imperial year");
Check(calendar.Build(2579, 12, 30, null, null, null).Days.Count == 30, "Imperial leap year uses correct underlying year");
Check(Month(1405, 7, now: new DateTimeOffset(2026, 10, 4, 21, 0, 0, TimeSpan.Zero)).Today == new DateOnly(2026, 10, 5), "Iran timezone midnight");
var ongoing = HeritageCalendarService.SolarDate(1405, 2, 13);
var ongoingModel = Month(1405, 2, query: "میدیوزرم", now: new DateTimeOffset(ongoing.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));
Check(ongoingModel.Upcoming[0].Date < ongoing && ongoingModel.Upcoming[0].EndDate >= ongoing, "Ongoing multi-day festival stays upcoming");
Check(ongoingModel.Days.Single(d => d.Number == 13).Events.Any(e => e.Event.Id == "maidyozarem"), "Multi-day festival marks every day");
Check(!calendar.Occurrences(1300, "named").Any(e => e.Event.Id == "imperial-coronation"), "No anniversary before historical event");
Check(HeritageCalendarCatalog.Events.Count == 38, "Catalog count");
Check(HeritageCalendarCatalog.Events.Select(e => e.Id).Distinct().Count() == 38, "Unique catalog IDs");
Check(HeritageCalendarCatalog.Events.All(e => Uri.TryCreate(e.SourceUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https" && e.SourceTitle.Length > 0), "Source on every event");
foreach (var year in Enumerable.Range(1300, 201))
{
    var lastDay = HeritageCalendarService.SolarDate(year + 1, 1, 1).AddDays(-1);
    var fire = Event(year, "chaharshanbe-suri");
    Check(fire.Date.DayOfWeek == DayOfWeek.Tuesday, $"Wednesday eve weekday {year}");
    Check(fire.Date.AddDays(1).DayOfWeek == DayOfWeek.Wednesday && fire.Date.AddDays(1) <= lastDay && fire.Date.AddDays(8) > lastDay, $"Last Wednesday boundary {year}");
    var lastFive = Event(year, "hamaspathmaidyem");
    Check(lastFive.EndDate == lastDay && lastFive.EndDate.DayNumber - lastFive.Date.DayNumber == 4, $"Five year-end days {year}");
    foreach (var occurrence in calendar.Occurrences(year, "named"))
        Check(pc.GetYear(occurrence.Date.ToDateTime(TimeOnly.MinValue)) == year, $"Occurrence stays in selected year {year}");
    var birthday = calendar.Occurrences(year, "named").SingleOrDefault(e => e.Event.Id == "farah-birthday");
    if (birthday is not null)
        Check(birthday.Date.Month == 10 && birthday.Date.Day == 14, $"Gregorian recurrence {year}");
}
foreach (var invalid in new[] { (1299, 1, 1), (1501, 1, 1), (1405, 13, 1), (1400, 12, 30), (1405, 1, 0) })
{
    var rejected = false;
    try { Month(invalid.Item1, invalid.Item2, invalid.Item3); }
    catch (ArgumentOutOfRangeException) { rejected = true; }
    Check(rejected, "Invalid date rejected");
}
Console.WriteLine($"PASS: {checks} calendar assertions");
