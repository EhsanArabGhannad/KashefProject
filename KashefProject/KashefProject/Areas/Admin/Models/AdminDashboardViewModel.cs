using KashefProject.Data;

namespace KashefProject.Areas.Admin.Models;

public sealed class AdminDashboardViewModel
{
    public long RevenueThisMonthCents { get; init; }
    public int PaidOrdersThisMonth { get; init; }
    public int AwaitingFulfillment { get; init; }
    public int PendingPayments { get; init; }
    public int PublishedProducts { get; init; }
    public int NewInquiries { get; init; }
    public bool EmailConfigured { get; init; }
    public IReadOnlyList<StoreOrder> RecentOrders { get; init; } = [];
}
