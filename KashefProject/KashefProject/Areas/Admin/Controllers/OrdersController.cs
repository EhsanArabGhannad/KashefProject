using KashefProject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin"), Route("admin/orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(StoreDbContext db) : Controller
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
}
