using KashefProject.Areas.Admin.Models;
using KashefProject.Data;
using KashefProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin"), Route("admin/calendar-events")]
public sealed class CalendarEventsController(StoreDbContext db, IImageStorage images,
    ILogger<CalendarEventsController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await db.CalendarEvents.AsNoTracking()
        .OrderBy(item => item.Month).ThenBy(item => item.Day).ThenBy(item => item.Id).ToListAsync());

    [HttpGet("create")]
    public IActionResult Create() => View("Editor", new CalendarEventEditorViewModel());

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.CalendarEvents.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        return item is null ? NotFound() : View("Editor", new CalendarEventEditorViewModel
        {
            Id = item.Id, Revision = item.Revision, TitleFa = item.TitleFa, TitleEn = item.TitleEn,
            DescriptionFa = item.DescriptionFa, DescriptionEn = item.DescriptionEn,
            NoteFa = item.NoteFa, NoteEn = item.NoteEn, Category = item.Category, Rule = item.Rule,
            Month = item.Month, Day = item.Day, Duration = item.Duration,
            HistoricalYear = item.HistoricalYear, OnlyImperialYear = item.OnlyImperialYear,
            IsPublished = item.IsPublished, ExistingImagePath = item.ImagePath
        });
    }

    [HttpPost("create"), ValidateAntiForgeryToken, RequestSizeLimit(9 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 9 * 1024 * 1024)]
    public Task<IActionResult> Create(CalendarEventEditorViewModel model, CancellationToken cancellationToken) => Save(null, model, cancellationToken);

    [HttpPost("{id:int}/edit"), ValidateAntiForgeryToken, RequestSizeLimit(9 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 9 * 1024 * 1024)]
    public Task<IActionResult> Edit(int id, CalendarEventEditorViewModel model, CancellationToken cancellationToken) => Save(id, model, cancellationToken);

    private async Task<IActionResult> Save(int? id, CalendarEventEditorViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != (id ?? 0)) return BadRequest();
        var item = id.HasValue ? await db.CalendarEvents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) : new CalendarEvent { Key = "event-" + Guid.NewGuid().ToString("N") };
        if (item is null) return NotFound();
        model.ExistingImagePath = item.ImagePath;
        if (id.HasValue && item.Revision != model.Revision)
            ModelState.AddModelError("", "This event was changed by another editor. Reload the page before saving again.");
        if (model.Image is not null)
        {
            try { await CalendarImageValidator.ValidateAsync(model.Image, cancellationToken); }
            catch (InvalidOperationException error) { ModelState.AddModelError(nameof(model.Image), error.Message); }
        }
        if (!ModelState.IsValid) return View("Editor", model);

        var previousImage = item.ImagePath;
        string? uploaded = null;
        try
        {
            if (model.Image is not null) uploaded = await images.SaveAsync(model.Image, cancellationToken);
            item.TitleFa = model.TitleFa.Trim(); item.TitleEn = model.TitleEn.Trim();
            item.DescriptionFa = model.DescriptionFa.Trim(); item.DescriptionEn = model.DescriptionEn.Trim();
            item.NoteFa = model.NoteFa?.Trim() ?? ""; item.NoteEn = model.NoteEn?.Trim() ?? "";
            item.Category = model.Category; item.Rule = model.Rule; item.Month = model.Month; item.Day = model.Day;
            item.Duration = model.Duration; item.HistoricalYear = model.HistoricalYear;
            item.OnlyImperialYear = model.OnlyImperialYear; item.IsPublished = model.IsPublished;
            item.ImagePath = uploaded ?? (model.RemoveImage ? null : previousImage);
            item.Revision = Guid.NewGuid().ToString("N");
            if (!id.HasValue) db.CalendarEvents.Add(item);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (uploaded is not null) await CleanImage(uploaded);
            ModelState.AddModelError("", "This event changed while you were saving. Reload and try again.");
            return View("Editor", model);
        }
        catch (OperationCanceledException)
        {
            if (uploaded is not null) await CleanImage(uploaded);
            throw;
        }
        catch (Exception error) when (error is InvalidOperationException or IOException or DbUpdateException)
        {
            if (uploaded is not null) await CleanImage(uploaded);
            logger.LogError(error, "Unable to save calendar event {Id}", id);
            ModelState.AddModelError("", "The event could not be saved. Please try again.");
            return View("Editor", model);
        }
        if (previousImage is not null && previousImage != item.ImagePath) await CleanImage(previousImage);
        TempData["StatusMessage"] = id.HasValue ? "Calendar event updated." : "Calendar event added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.CalendarEvents.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost("{id:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string revision)
    {
        var item = await db.CalendarEvents.SingleOrDefaultAsync(item => item.Id == id);
        if (item is null) return NotFound();
        if (item.Revision != revision) return Conflict("This event changed. Reload before deleting.");
        db.CalendarEvents.Remove(item);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("This event changed. Reload before deleting."); }
        if (item.ImagePath is not null) await CleanImage(item.ImagePath);
        TempData["StatusMessage"] = "Calendar event deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CleanImage(string path)
    {
        try { await images.DeleteAsync(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { logger.LogWarning(error, "Calendar image cleanup deferred."); }
    }
}
