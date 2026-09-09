using KashefProject.Data;
using KashefProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace KashefProject.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin"), Route("admin/orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(StoreDbContext db, StripePaymentService payments, ILogger<OrdersController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Clamp(page, 1, 10000);
        var count = await db.Orders.CountAsync();
        ViewData["Page"] = page;
        ViewData["HasNext"] = count > page * 30;
        return View(await db.Orders.AsNoTracking().OrderByDescending(order => order.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders.AsNoTracking().Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost("{id:int}/cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        // Never offer a manual 'paid' action; payment status will come from the provider.
        await db.Orders.Where(order => order.Id == id && order.Status == OrderStatus.PendingPayment)
            .ExecuteUpdateAsync(update => update.SetProperty(order => order.Status, OrderStatus.Cancelled));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/sync-payment"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncPayment(int id)
    {
        try
        {
            TempData["OrderNotice"] = await payments.SyncOrderAsync(id)
                ? "Stripe confirmed this payment."
                : "Stripe has not confirmed a completed payment for this order.";
        }
        catch (StripeException exception)
        {
            logger.LogWarning(exception, "Could not synchronize Stripe payment for order {OrderId}.", id);
            TempData["OrderNotice"] = "Stripe could not be reached. Please try again in a moment.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
