using KashefProject.Data;
using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Controllers;

[Route("checkout")]
[Authorize]
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
        // The verified account email is authoritative; ignore any posted replacement.
        ModelState.Remove(nameof(model.Email));
        model.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Challenge();
        var order = await db.Orders.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.CustomerUserId == userId);
        if (order is null) return NotFound();
        ViewData["PaymentsEnabled"] = payments.IsConfigured;
        return View(order);
    }

    [HttpPost("pay/{reference}"), ValidateAntiForgeryToken, EnableRateLimiting("checkout")]
    public async Task<IActionResult> Pay(string reference)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || !payments.IsConfigured) return NotFound();
        var order = await db.Orders.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.CustomerUserId == userId);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.PendingPayment) return RedirectToAction(nameof(Saved), new { reference });
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var hash = owner.GetHash();
        if (userId is null || hash is null || string.IsNullOrWhiteSpace(sessionId)) return NotFound();
        var savedOrder = await db.Orders.AsNoTracking().SingleOrDefaultAsync(item =>
            item.StripeCheckoutSessionId == sessionId && item.CustomerUserId == userId && item.OwnerHash == hash);
        if (savedOrder is null) return NotFound();
        if (savedOrder.Status != OrderStatus.PendingPayment)
            return RedirectToAction(nameof(Saved), new { reference = savedOrder.Reference });
        try
        {
            var order = await payments.ConfirmReturnAsync(sessionId, hash);
            if (order is null)
                TempData["PaymentError"] = "We could not check your payment status yet. If you completed payment, check its status before trying to pay again.";
        }
        catch (Exception exception) when (exception is Stripe.StripeException or HttpRequestException)
        {
            logger.LogError(exception, "Stripe return verification failed for session {SessionId}.", sessionId);
            TempData["PaymentError"] = "We could not check your payment status yet. If you completed payment, check its status before trying to pay again.";
        }
        return RedirectToAction(nameof(Saved), new { reference = savedOrder.Reference });
    }

    [HttpPost("status/{reference}"), ValidateAntiForgeryToken, EnableRateLimiting("checkout")]
    public async Task<IActionResult> CheckStatus(string reference)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Challenge();
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Reference == reference && item.CustomerUserId == userId);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.PendingPayment)
            return RedirectToAction(nameof(Saved), new { reference });
        if (!payments.IsConfigured || string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId))
        {
            TempData["PaymentError"] = "Payment status checking is not available for this order yet. Your order remains saved in your account.";
            return RedirectToAction(nameof(Saved), new { reference });
        }
        try
        {
            if (!await payments.SyncOrderAsync(order.Id))
                TempData["PaymentNotice"] = "Stripe has not confirmed payment for this order yet. If you just completed payment, wait a moment and check again.";
        }
        catch (Exception exception) when (exception is Stripe.StripeException or HttpRequestException)
        {
            logger.LogError(exception, "Stripe status check failed for order {Reference}.", reference);
            TempData["PaymentError"] = "We could not check your payment status yet. Please try checking again before making another payment.";
        }
        return RedirectToAction(nameof(Saved), new { reference });
    }
}
