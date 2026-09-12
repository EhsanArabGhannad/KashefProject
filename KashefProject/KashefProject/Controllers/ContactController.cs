using KashefProject.Data;
using KashefProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KashefProject.Controllers;

[Route("contact")]
public class ContactController(StoreDbContext db) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new ContactInquiryViewModel());

    [HttpPost(""), ValidateAntiForgeryToken, EnableRateLimiting("contact")]
    public async Task<IActionResult> Index(ContactInquiryViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Website)) return RedirectToAction(nameof(Thanks));
        if (!ContactInquiryViewModel.Interests.Contains(model.Interest))
            ModelState.AddModelError(nameof(model.Interest), "Choose one of the available topics.");
        if (!ModelState.IsValid) return View(model);

        var inquiry = new ContactInquiry
        {
            Reference = $"INQ-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Interest = model.Interest,
            Message = model.Message.Trim()
        };
        db.ContactInquiries.Add(inquiry);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Thanks), new { reference = inquiry.Reference });
    }

    [HttpGet("thanks")]
    public IActionResult Thanks(string? reference)
    {
        ViewData["Reference"] = reference;
        return View();
    }
}
