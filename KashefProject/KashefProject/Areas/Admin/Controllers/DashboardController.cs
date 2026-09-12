using KashefProject.Areas.Admin.Models;
using KashefProject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using KashefProject.Services;

namespace KashefProject.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin"), Route("admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DashboardController(StoreDbContext db, IOptions<EmailDeliveryOptions> emailOptions) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var paidThisMonth = db.Orders.AsNoTracking()
            .Where(order => order.Status == OrderStatus.Paid && order.PaidUtc >= monthStart);

        return View(new AdminDashboardViewModel
        {
            RevenueThisMonthCents = await paidThisMonth.SumAsync(order => order.PaymentReceivedCents ?? 0),
            PaidOrdersThisMonth = await paidThisMonth.CountAsync(),
            AwaitingFulfillment = await db.Orders.CountAsync(order => order.Status == OrderStatus.Paid &&
                order.FulfillmentStatus != FulfillmentStatus.Delivered),
            PendingPayments = await db.Orders.CountAsync(order => order.Status == OrderStatus.PendingPayment),
            PublishedProducts = await db.Products.CountAsync(product => product.IsPublished),
            NewInquiries = await db.ContactInquiries.CountAsync(inquiry => inquiry.Status == ContactInquiryStatus.New),
            EmailConfigured = emailOptions.Value.IsConfigured,
            RecentOrders = await db.Orders.AsNoTracking().OrderByDescending(order => order.Id).Take(6).ToListAsync()
        });
    }
}
