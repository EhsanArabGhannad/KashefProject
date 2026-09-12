using KashefProject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin"), Route("admin/inquiries")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InquiriesController(StoreDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        page = Math.Max(1, page);
        q = q?.Trim();
        status = status?.Trim().ToLowerInvariant();
        var inquiries = db.ContactInquiries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            inquiries = inquiries.Where(item => item.Reference.Contains(q) || item.Name.Contains(q) ||
                item.Email.Contains(q) || item.Interest.Contains(q));
        inquiries = status switch
        {
            "new" => inquiries.Where(item => item.Status == ContactInquiryStatus.New),
            "read" => inquiries.Where(item => item.Status == ContactInquiryStatus.Read),
            "archived" => inquiries.Where(item => item.Status == ContactInquiryStatus.Archived),
            _ => inquiries.Where(item => item.Status != ContactInquiryStatus.Archived)
        };
        const int pageSize = 30;
        var model = await inquiries.OrderByDescending(item => item.Id).Skip((page - 1) * pageSize).Take(pageSize + 1).ToListAsync();
        ViewData["Query"] = q ?? "";
        ViewData["Status"] = status ?? "";
        ViewData["Page"] = page;
        ViewData["HasNext"] = model.Count > pageSize;
        return View(model.Take(pageSize).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var inquiry = await db.ContactInquiries.SingleOrDefaultAsync(item => item.Id == id);
        if (inquiry is null) return NotFound();
        if (inquiry.Status == ContactInquiryStatus.New)
        {
            inquiry.Status = ContactInquiryStatus.Read;
            await db.SaveChangesAsync();
        }
        return View(inquiry);
    }

    [HttpPost("{id:int}/archive"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        await db.ContactInquiries.Where(item => item.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, ContactInquiryStatus.Archived));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/reopen"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id)
    {
        await db.ContactInquiries.Where(item => item.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, ContactInquiryStatus.New));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/retry"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id)
    {
        var now = DateTime.UtcNow;
        await db.ContactInquiries
            .Where(item => item.Id == id && item.NotificationStatus == ContactInquiryNotificationStatus.Failed)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.NotificationStatus, ContactInquiryNotificationStatus.Pending)
                .SetProperty(item => item.AttemptCount, 0)
                .SetProperty(item => item.NextAttemptUtc, now)
                .SetProperty(item => item.LastError, (string?)null));
        return RedirectToAction(nameof(Details), new { id });
    }
}
