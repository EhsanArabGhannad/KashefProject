using System.ComponentModel.DataAnnotations;
using KashefProject.Areas.Admin.Controllers;
using KashefProject.Areas.Admin.Models;
using KashefProject.Data;
using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); checks++; }
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<StoreDbContext>().UseSqlite(connection).Options;
await using var db = new StoreDbContext(options);
await db.Database.MigrateAsync();
await DatabaseInitializer.SeedCalendarAsync(db);
Check(await db.CalendarEvents.CountAsync() == 38, "Initial 38 events imported");
await DatabaseInitializer.SeedCalendarAsync(db);
Check(await db.CalendarEvents.CountAsync() == 38, "Import is idempotent");
var calendar = new HeritageCalendarService();
var definitions = (await db.CalendarEvents.AsNoTracking().ToListAsync()).Select(e => e.ToDefinition()).ToArray();
foreach (var lang in new[] { "en", "fa" })
{
    var seeded = calendar.Build(2585, 8, 4, null, null, null, language: lang, eventDefinitions: definitions);
    var original = calendar.Build(2585, 8, 4, null, null, null, language: lang);
    Check(seeded.YearEvents.Select(e => (e.Event.Title, e.Event.Description, e.Event.Note, e.Date, e.EndDate))
        .SequenceEqual(original.YearEvents.Select(e => (e.Event.Title, e.Event.Description, e.Event.Note, e.Date, e.EndDate))), "Imported bilingual recurrence parity " + lang);
}
var sample = definitions[0] with { Id = "custom", Title = "سفارشی", Description = "شرح فارسی", TitleEn = "Custom", DescriptionEn = "English copy", NoteEn = "Edited note", ImagePath = "/uploads/test.png", OnlyImperialYear = 2585, Month = 1, Day = 1 };
Check(calendar.Build(2585, 1, 1, null, "English copy", null, language: "en", eventDefinitions: [sample]).DayEvents.Single().Event.Title == "Custom", "DB English content overrides static translations");
Check(calendar.Build(2585, 1, 1, null, "سفارشی", null, language: "en", eventDefinitions: [sample]).DayEvents.Single().Event.ImagePath == "/uploads/test.png", "Cross-language search and image preserved");
Check(calendar.Build(2586, 1, 1, null, null, null, eventDefinitions: [sample]).YearEvents.Count == 0, "Single-year events do not repeat");
var spanning = sample with { OnlyImperialYear = null, Month = 12, Day = 29, Duration = 5 };
Check(calendar.Build(2586, 1, 1, null, null, null, eventDefinitions: [spanning]).DayEvents.Count == 1, "Multi-day event crosses Nowruz");
var leap = sample with { OnlyImperialYear = null, Month = 12, Day = 30 };
Check(calendar.Build(2580, 12, 29, null, null, null, eventDefinitions: [leap]).YearEvents.Count == 0, "Solar leap-only date skips common year");
Check(calendar.Build(2579, 12, 30, null, null, null, eventDefinitions: [leap]).DayEvents.Count == 1, "Solar leap-only date occurs in leap year");
var gregorianLeap = sample with { OnlyImperialYear = null, Rule = HeritageDateRule.Gregorian, Month = 2, Day = 29 };
Check(calendar.Build(2585, 12, 1, null, null, null, eventDefinitions: [gregorianLeap]).YearEvents.Count == 0, "Gregorian leap-only date skips common year");
Check(calendar.Build(2586, 12, 1, null, null, null, eventDefinitions: [gregorianLeap]).YearEvents.Count == 1, "Gregorian leap-only date appears in leap year");
CalendarEventEditorViewModel Valid() => new() { TitleEn = "New", TitleFa = "نو", DescriptionEn = "Copy", DescriptionFa = "شرح" };
bool Validates(CalendarEventEditorViewModel model) => Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
Check(Validates(Valid()), "Valid editor model");
var invalid = Valid(); invalid.Month = 7; invalid.Day = 31; Check(!Validates(invalid), "Invalid solar day rejected");
invalid = Valid(); invalid.Rule = HeritageDateRule.Gregorian; invalid.Month = 2; invalid.Day = 30; Check(!Validates(invalid), "Invalid Gregorian day rejected");
invalid = Valid(); invalid.Rule = (HeritageDateRule)999; Check(!Validates(invalid), "Invalid rule rejected");
invalid = Valid(); invalid.Category = "evil"; Check(!Validates(invalid), "Invalid category rejected");
invalid = Valid(); invalid.TitleEn = " "; Check(!Validates(invalid), "Whitespace title rejected");
invalid = Valid(); invalid.OnlyImperialYear = 2580; invalid.Month = 12; invalid.Day = 30; Check(!Validates(invalid), "Impossible single-year solar date rejected");
invalid = Valid(); invalid.OnlyImperialYear = 2585; invalid.Rule = HeritageDateRule.Gregorian; invalid.Month = 2; invalid.Day = 29; Check(!Validates(invalid), "Impossible single-year Gregorian date rejected");
invalid.OnlyImperialYear = 2586; Check(Validates(invalid), "Valid single-year Gregorian leap date accepted");
var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jR1cAAAAASUVORK5CYII=");
IFormFile File(byte[] bytes, string name) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Image", name);
await CalendarImageValidator.ValidateAsync(File(png, "image.png"), default); checks++;
foreach (var file in new[] { File(png, "fake.jpg"), File(new byte[100], "fake.png"), File(png, "image.svg"), File(new byte[2], "tiny.png"), new FormFile(new MemoryStream(), 0, 8 * 1024 * 1024 + 1, "Image", "large.png") })
{
    var rejected = false;
    try { await CalendarImageValidator.ValidateAsync(file, default); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "Invalid image rejected " + file.FileName);
}
var first = await db.CalendarEvents.FirstAsync();
await using (var other = new StoreDbContext(options))
{
    var newer = await other.CalendarEvents.FindAsync(first.Id);
    newer!.TitleEn = "Other editor"; newer.Revision = Guid.NewGuid().ToString("N"); await other.SaveChangesAsync();
}
first.TitleEn = "Stale update"; first.Revision = Guid.NewGuid().ToString("N");
var conflicted = false;
try { await db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflicted = true; }
Check(conflicted, "Concurrent edits cannot silently overwrite");
db.ChangeTracker.Clear();
await db.CalendarEvents.ExecuteDeleteAsync();
await using (var restarted = new StoreDbContext(options))
{
    await DatabaseInitializer.SeedCalendarAsync(restarted);
    Check(await restarted.CalendarEvents.CountAsync() == 0, "Deleted catalogue stays deleted after restart");
}
Check(typeof(CalendarEventsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles == "Admin", "Admin-only controller");
Console.WriteLine($"PASS: {checks} calendar management assertions");
