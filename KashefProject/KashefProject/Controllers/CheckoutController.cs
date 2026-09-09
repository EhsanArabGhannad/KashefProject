using KashefProject.Data;
using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Controllers;

[Route("checkout")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CheckoutController(
    CheckoutService checkout,
    StripePaymentService payments,
    StoreDbContext db,
    CartOwner owner,
    ILogger<CheckoutController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = await checkout.PrepareAsync();
        ViewData["PaymentsEnabled"] = payments.IsConfigured;
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
            if (result.Order is not null)
            {
                if (payments.IsConfigured)
                {
                    try
                    {
                        var session = await payments.StartAsync(result.Order);
                        return Redirect(session.Url);
                    }
                    catch (Exception exception) when (exception is Stripe.StripeException or InvalidOperationException)
                    {
                        logger.LogError(exception, "Stripe Checkout could not start for order {Reference}.", result.Order.Reference);
                        TempData["PaymentError"] = "We saved your order, but the secure payment page could not be opened. Please try payment again.";
                    }
                }
                return RedirectToAction(nameof(Saved), new { reference = result.Order.Reference });
            }
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
        ViewData["PaymentsEnabled"] = payments.IsConfigured;
        return View(prepared);
    }

    [HttpGet("saved/{reference}")]
    public async Task<IActionResult> Saved(string reference)
    {
        var hash = owner.GetHash();
        if (hash is null) return NotFound();
        var order = await db.Orders.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.OwnerHash == hash);
        if (order is null) return NotFound();
        ViewData["PaymentsEnabled"] = payments.IsConfigured;
        return View(order);
    }

    [HttpPost("pay/{reference}"), ValidateAntiForgeryToken, EnableRateLimiting("checkout")]
    public async Task<IActionResult> Pay(string reference)
    {
        var hash = owner.GetHash();
        if (hash is null || !payments.IsConfigured) return NotFound();
        var order = await db.Orders.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.OwnerHash == hash);
        if (order is null) return NotFound();
        if (order.Status == OrderStatus.Paid) return RedirectToAction(nameof(Saved), new { reference });
        if (order.Status == OrderStatus.Cancelled) return BadRequest();
        try
        {
            var session = await payments.StartAsync(order);
            return Redirect(session.Url);
        }
        catch (Exception exception) when (exception is Stripe.StripeException or InvalidOperationException)
        {
            logger.LogError(exception, "Stripe Checkout retry failed for order {Reference}.", order.Reference);
            TempData["PaymentError"] = "The secure payment page is temporarily unavailable. Please try again.";
            return RedirectToAction(nameof(Saved), new { reference });
        }
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery(Name = "session_id")] string sessionId)
    {
        var hash = owner.GetHash();
        if (hash is null || string.IsNullOrWhiteSpace(sessionId)) return NotFound();
        try
        {
            var order = await payments.ConfirmReturnAsync(sessionId, hash);
            return order is null ? NotFound() : RedirectToAction(nameof(Saved), new { reference = order.Reference });
        }
        catch (Stripe.StripeException exception)
        {
            logger.LogError(exception, "Stripe return verification failed for session {SessionId}.", sessionId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
