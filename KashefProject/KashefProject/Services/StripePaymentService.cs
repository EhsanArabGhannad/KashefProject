using KashefProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace KashefProject.Services;

public sealed class StripePaymentOptions
{
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
}

public sealed record StripeCheckoutStart(string Url, string SessionId);

public sealed class StripePaymentService(
    StoreDbContext db,
    IOptions<StripePaymentOptions> configuredOptions,
    IConfiguration configuration,
    ILogger<StripePaymentService> logger)
{
    private readonly StripePaymentOptions options = configuredOptions.Value;

    public bool IsConfigured =>
        options.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal) ||
        options.SecretKey.StartsWith("rk_test_", StringComparison.Ordinal) ||
        options.SecretKey.StartsWith("sk_live_", StringComparison.Ordinal) ||
        options.SecretKey.StartsWith("rk_live_", StringComparison.Ordinal);

    public bool IsWebhookConfigured => IsConfigured && options.WebhookSecret.StartsWith("whsec_", StringComparison.Ordinal);

    private SessionService Sessions()
    {
        if (!IsConfigured) throw new InvalidOperationException("Stripe payments are not configured.");
        return new SessionService(new StripeClient(options.SecretKey));
    }

    private string PublicUrl()
    {
        var value = configuration["Site:PublicUrl"]?.TrimEnd('/');
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Site:PublicUrl must be an absolute HTTPS URL before Stripe Checkout can start.");
        return uri.GetLeftPart(UriPartial.Authority);
    }

    public async Task<StripeCheckoutStart> StartAsync(StoreOrder order)
    {
        if (order.Status == OrderStatus.Cancelled) throw new InvalidOperationException("A cancelled order cannot be paid.");
        if (order.Status == OrderStatus.Paid) throw new InvalidOperationException("This order is already paid.");
        if (order.Lines.Count == 0) throw new InvalidOperationException("This order has no items.");

        var sessions = Sessions();
        if (!string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId))
        {
            var existing = await sessions.GetAsync(order.StripeCheckoutSessionId);
            if (existing.Status == "open" && !string.IsNullOrWhiteSpace(existing.Url))
                return new(existing.Url, existing.Id);
            if (existing.PaymentStatus == "paid")
            {
                await MarkPaidAsync(existing);
                throw new InvalidOperationException("This order is already paid.");
            }
        }

        var site = PublicUrl();
        var create = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{site}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{site}/checkout/saved/{Uri.EscapeDataString(order.Reference)}?payment=cancelled",
            ClientReferenceId = order.Reference,
            CustomerEmail = order.Email,
            SubmitType = "pay",
            Metadata = new Dictionary<string, string> { ["order_reference"] = order.Reference },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string> { ["order_reference"] = order.Reference }
            },
            LineItems = order.Lines.Select(line => new SessionLineItemOptions
            {
                Quantity = line.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = line.UnitPriceCents,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = line.ProductName,
                        Description = $"{line.Size} · {line.Finish}"
                    }
                }
            }).ToList()
        };
        var idempotency = $"craftisma-{order.Reference}-{order.StripeCheckoutSessionId ?? "first"}";
        var session = await sessions.CreateAsync(create, new RequestOptions { IdempotencyKey = idempotency });
        if (string.IsNullOrWhiteSpace(session.Url)) throw new InvalidOperationException("Stripe did not return a checkout URL.");

        await db.Orders.Where(item => item.Id == order.Id && item.Status == OrderStatus.PendingPayment)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.StripeCheckoutSessionId, session.Id));
        return new(session.Url, session.Id);
    }

    public async Task<StoreOrder?> ConfirmReturnAsync(string sessionId, string ownerHash)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId)) return null;
        var order = await db.Orders.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.StripeCheckoutSessionId == sessionId && item.OwnerHash == ownerHash);
        if (order is null) return null;
        var session = await Sessions().GetAsync(sessionId);
        await MarkPaidAsync(session);
        return await db.Orders.AsNoTracking().Include(item => item.Lines).SingleAsync(item => item.Id == order.Id);
    }

    public async Task<bool> SyncOrderAsync(int orderId)
    {
        if (!IsConfigured) return false;
        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == orderId);
        if (order is null || order.Status == OrderStatus.Cancelled || string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId))
            return false;
        if (order.Status == OrderStatus.Paid) return true;

        var session = await Sessions().GetAsync(order.StripeCheckoutSessionId);
        await MarkPaidAsync(session);
        return await db.Orders.AsNoTracking().AnyAsync(item => item.Id == orderId && item.Status == OrderStatus.Paid);
    }

    public Event VerifyWebhook(string json, string signature) =>
        EventUtility.ConstructEvent(json, signature, options.WebhookSecret);

    public async Task MarkPaidAsync(Session session)
    {
        if (session.PaymentStatus != "paid" && session.PaymentStatus != "no_payment_required") return;
        if (!string.Equals(session.Currency, "usd", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: unexpected currency {Currency}.", session.Id, session.Currency);
            return;
        }

        var reference = session.ClientReferenceId;
        if (string.IsNullOrWhiteSpace(reference) && session.Metadata.TryGetValue("order_reference", out var metadataReference))
            reference = metadataReference;
        if (string.IsNullOrWhiteSpace(reference)) return;

        var order = await db.Orders.SingleOrDefaultAsync(item => item.Reference == reference);
        if (order is null || order.Status == OrderStatus.Paid) return;
        if (!string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId) && order.StripeCheckoutSessionId != session.Id)
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: order {Reference} is linked to another session.", session.Id, reference);
            return;
        }
        if (session.AmountTotal is null || session.AmountTotal < order.SubtotalCents)
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: amount {Amount} is below order subtotal {Subtotal}.", session.Id, session.AmountTotal, order.SubtotalCents);
            return;
        }

        order.StripeCheckoutSessionId = session.Id;
        order.StripePaymentIntentId = session.PaymentIntentId;
        order.PaymentReceivedCents = session.AmountTotal;
        order.ShippingCents = session.TotalDetails?.AmountShipping;
        order.TaxCents = session.TotalDetails?.AmountTax;
        order.TotalCents = session.AmountTotal;
        order.PaidUtc = DateTime.UtcNow;
        order.Status = OrderStatus.Paid;
        await db.SaveChangesAsync();
    }
}
