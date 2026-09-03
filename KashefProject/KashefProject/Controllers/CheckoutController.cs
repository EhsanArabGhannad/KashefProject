using KashefProject.Data;
using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Controllers;

[Route("checkout")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CheckoutController(CheckoutService checkout, StoreDbContext db, CartOwner owner) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = await checkout.PrepareAsync();
        return model is null ? RedirectToAction("Index", "Cart") : View(model);
    }

    [HttpPost(""), ValidateAntiForgeryToken, EnableRateLimiting("checkout")]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        if (!UsStates.All.ContainsKey(model.State ?? "")) ModelState.AddModelError(nameof(model.State), "Choose a valid US state.");
        if (ModelState.IsValid)
        {
            var result = await checkout.SubmitAsync(model);
            if (result.Order is not null) return RedirectToAction(nameof(Saved), new { reference = result.Order.Reference });
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        var prepared = await checkout.PrepareAsync(model);
        if (prepared is null)
        {
            TempData["CartError"] = "Your bag is empty or contains unavailable items. Please review it before continuing.";
            return RedirectToAction("Index", "Cart");
        }
        // Render the fresh review token, not the stale posted value held by MVC.
        ModelState.Remove(nameof(model.ReviewToken));
        return View(prepared);
    }

    [HttpGet("saved/{reference}")]
    public async Task<IActionResult> Saved(string reference)
    {
        var hash = owner.GetHash();
        if (hash is null) return NotFound();
        var order = await db.Orders.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.OwnerHash == hash);
        return order is null ? NotFound() : View(order);
    }
}
