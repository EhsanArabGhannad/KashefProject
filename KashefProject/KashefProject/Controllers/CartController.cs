using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KashefProject.Controllers;

[Route("cart")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartController(ShoppingService shopping) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await shopping.GetSummaryAsync());

    [HttpPost("add"), ValidateAntiForgeryToken, EnableRateLimiting("cart")]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var error = ModelState.IsValid ? await shopping.AddAsync(productId, quantity) : "Choose a valid quantity.";
        TempData[error is null ? "CartMessage" : "CartError"] = error ?? "Added to your bag.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update"), ValidateAntiForgeryToken, EnableRateLimiting("cart")]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var error = ModelState.IsValid ? await shopping.UpdateAsync(productId, quantity) : "Choose a valid quantity.";
        TempData[error is null ? "CartMessage" : "CartError"] = error ?? "Your bag has been updated.";
        return RedirectToAction(nameof(Index));
    }
}
