using KashefProject.Services;
using KashefProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("calendar")]
public sealed class HeritageCalendarController(HeritageCalendarService calendar, StoreDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? year, int? month, int? day, string? category, string? q, string? convention, string? lang)
    {
        var fa = lang == "fa";
        if (!ModelState.IsValid) return BadRequest(fa ? "تاریخ واردشده معتبر نیست." : "The entered date is invalid.");
        // Preserve links from the first Solar Hijri preview, but use imperial years in all new URLs.
        if (year is >= 1300 and <= 1500)
            return RedirectToAction(nameof(Index), new { year = year + Models.HeritageCalendarText.ImperialYearOffset, month, day, category, q, convention, lang });
        var events = await db.CalendarEvents.AsNoTracking().Where(item => item.IsPublished).OrderBy(item => item.Id).ToListAsync();
        try { return View(calendar.Build(year, month, day, category, q, convention, language: lang, eventDefinitions: events.Select(item => item.ToDefinition()))); }
        catch (ArgumentOutOfRangeException) { return BadRequest(fa ? "تاریخ واردشده معتبر نیست. سال شاهنشاهی باید بین ۲۴۸۰ و ۲۶۸۰ باشد." : "Invalid date. The Imperial year must be between 2480 and 2680."); }
    }
}
