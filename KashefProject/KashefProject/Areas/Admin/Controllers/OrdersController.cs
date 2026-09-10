using KashefProject.Data;
using KashefProject.Areas.Admin.Models;
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
    public async Task<IActionResult> Index(string? q, string? payment, string? fulfillment, int page = 1)
    {
        page = Math.Clamp(page, 1, 10000);
        var query = db.Orders.AsNoTracking().AsQueryable();
        q = q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(order => order.Reference.Contains(q) || order.FullName.Contains(q) ||
                order.Email.Contains(q) || (order.TrackingNumber != null && order.TrackingNumber.Contains(q)));

        query = payment?.ToLowerInvariant() switch
        {
            "paid" => query.Where(order => order.Status == OrderStatus.Paid),
            "pending" => query.Where(order => order.Status == OrderStatus.PendingPayment),
            "cancelled" => query.Where(order => order.Status == OrderStatus.Cancelled),
            _ => query
        };
        if (string.Equals(fulfillment, "open", StringComparison.OrdinalIgnoreCase))
            query = query.Where(order => order.Status == OrderStatus.Paid &&
                order.FulfillmentStatus != FulfillmentStatus.Delivered);
        else if (Enum.TryParse<FulfillmentStatus>(fulfillment, true, out var fulfillmentStatus))
            query = query.Where(order => order.Status == OrderStatus.Paid && order.FulfillmentStatus == fulfillmentStatus);

        var count = await query.CountAsync();
        ViewData["Page"] = page;
        ViewData["Query"] = q ?? "";
        ViewData["Payment"] = payment ?? "";
        ViewData["Fulfillment"] = fulfillment ?? "";
        ViewData["HasNext"] = count > page * 30;
        return View(await query.OrderByDescending(order => order.Id).Skip((page - 1) * 30).Take(30).ToListAsync());
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

    [HttpPost("{id:int}/fulfillment"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFulfillment(int id, FulfillmentUpdateViewModel model)
    {
        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == id);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Paid)
        {
            TempData["OrderNotice"] = "Fulfillment can only be updated after Stripe confirms payment.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (!Enum.IsDefined(model.FulfillmentStatus))
            ModelState.AddModelError(nameof(model.FulfillmentStatus), "Choose a valid fulfillment status.");

        model.TrackingCarrier = model.TrackingCarrier?.Trim();
        model.TrackingNumber = model.TrackingNumber?.Trim();
        model.AdminNotes = model.AdminNotes?.Trim();
        if ((model.FulfillmentStatus is FulfillmentStatus.Shipped or FulfillmentStatus.Delivered) &&
            string.IsNullOrWhiteSpace(model.TrackingNumber))
            ModelState.AddModelError(nameof(model.TrackingNumber), "Add a tracking number before marking this order shipped.");
        if (!ModelState.IsValid)
        {
            TempData["OrderNotice"] = string.Join(" ", ModelState.Values.SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage).Where(message => !string.IsNullOrWhiteSpace(message)));
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.FulfillmentStatus = model.FulfillmentStatus;
        order.TrackingCarrier = model.TrackingCarrier;
        order.TrackingNumber = model.TrackingNumber;
        order.AdminNotes = model.AdminNotes;
        if (model.FulfillmentStatus is FulfillmentStatus.Shipped or FulfillmentStatus.Delivered)
            order.ShippedUtc ??= now;
        if (model.FulfillmentStatus == FulfillmentStatus.Delivered)
            order.DeliveredUtc ??= now;
        await db.SaveChangesAsync();
        TempData["OrderNotice"] = $"Fulfillment updated to {model.FulfillmentStatus}.";
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
