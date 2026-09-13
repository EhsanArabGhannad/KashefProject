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
public sealed record StripeRefundOutcome(bool Complete, bool Pending, string Status);

public sealed class StripePaymentService(
    StoreDbContext db,
    IOptions<StripePaymentOptions> configuredOptions,
    FulfillmentPolicy fulfillment,
    OrderNotificationQueue notifications,
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

    private StripeClient Client()
    {
        if (!IsConfigured) throw new InvalidOperationException("Stripe payments are not configured.");
        return new StripeClient(options.SecretKey);
    }

    private SessionService Sessions() => new(Client());
    private RefundService Refunds() => new(Client());

    private string PublicUrl()
    {
        var value = configuration["Site:PublicUrl"]?.TrimEnd('/');
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Site:PublicUrl must be an absolute HTTPS URL before Stripe Checkout can start.");
        return uri.GetLeftPart(UriPartial.Authority);
    }

    public async Task<StripeCheckoutStart> StartAsync(StoreOrder order)
    {
        if (order.Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException(order.Status == OrderStatus.Paid
                ? "This order is already paid."
                : "This order cannot be paid.");
        if (order.Lines.Count == 0) throw new InvalidOperationException("This order has no items.");

        if (order.ShippingCents is null)
        {
            order.ShippingCents = fulfillment.ShippingFor(order.SubtotalCents);
            await db.SaveChangesAsync();
        }

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
            CustomerCreation = "always",
            ShippingAddressCollection = new SessionShippingAddressCollectionOptions
            {
                AllowedCountries = ["US"]
            },
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = fulfillment.AutomaticTaxEnabled },
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
                    TaxBehavior = "exclusive",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = line.ProductName,
                        Description = $"{line.Size} · {line.Finish}",
                        TaxCode = "txcd_99999999"
                    }
                }
            }).ToList(),
            ShippingOptions =
            [
                new SessionShippingOptionOptions
                {
                    ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                    {
                        Type = "fixed_amount",
                        DisplayName = order.ShippingCents == 0 ? "Free standard shipping" : "Standard shipping",
                        FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                        {
                            Amount = order.ShippingCents,
                            Currency = "usd"
                        },
                        TaxBehavior = "exclusive",
                        TaxCode = "txcd_92010001",
                        DeliveryEstimate = new SessionShippingOptionShippingRateDataDeliveryEstimateOptions
                        {
                            Minimum = new SessionShippingOptionShippingRateDataDeliveryEstimateMinimumOptions
                            {
                                Unit = "business_day", Value = 12
                            },
                            Maximum = new SessionShippingOptionShippingRateDataDeliveryEstimateMaximumOptions
                            {
                                Unit = "business_day", Value = 17
                            }
                        }
                    }
                }
            ]
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
        if (order is null || order.Status != OrderStatus.PendingPayment || string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId))
            return false;

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
        if (order is null || order.Status != OrderStatus.PendingPayment) return;
        if (!string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId) && order.StripeCheckoutSessionId != session.Id)
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: order {Reference} is linked to another session.", session.Id, reference);
            return;
        }
        var expectedShipping = order.ShippingCents ?? fulfillment.ShippingFor(order.SubtotalCents);
        var receivedShipping = session.TotalDetails?.AmountShipping ?? 0;
        if (receivedShipping != expectedShipping)
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: shipping {Shipping} does not match expected {ExpectedShipping}.", session.Id, receivedShipping, expectedShipping);
            return;
        }
        if (session.AmountTotal is null || session.AmountTotal < order.SubtotalCents + expectedShipping)
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: amount {Amount} is below the expected pre-tax total {ExpectedTotal}.", session.Id, session.AmountTotal, order.SubtotalCents + expectedShipping);
            return;
        }

        var shipping = session.CollectedInformation?.ShippingDetails;
        var address = shipping?.Address;
        if (address is null || !string.Equals(address.Country, "US", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(address.Line1) || string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.State) || string.IsNullOrWhiteSpace(address.PostalCode))
        {
            logger.LogWarning("Rejected Stripe session {SessionId}: a complete U.S. delivery address was not returned.", session.Id);
            return;
        }

        order.StripeCheckoutSessionId = session.Id;
        order.StripePaymentIntentId = session.PaymentIntentId;
        order.PaymentReceivedCents = session.AmountTotal;
        order.ShippingCents = receivedShipping;
        order.TaxCents = session.TotalDetails?.AmountTax;
        order.TotalCents = session.AmountTotal;
        if (!string.IsNullOrWhiteSpace(shipping?.Name)) order.FullName = shipping.Name;
        order.AddressLine1 = address.Line1;
        order.AddressLine2 = address.Line2;
        order.City = address.City;
        order.State = address.State;
        order.PostalCode = address.PostalCode;
        order.Country = "US";
        order.PaidUtc = DateTime.UtcNow;
        order.Status = OrderStatus.Paid;
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        await notifications.QueuePaymentAsync(order.Id);
        await transaction.CommitAsync();
    }

    public static bool IsRefundReason(string? reason) => reason is "requested_by_customer" or "duplicate" or "fraudulent";

    public async Task<StripeRefundOutcome> RefundOrderAsync(
        int orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!IsRefundReason(reason)) throw new InvalidOperationException("Choose a valid refund reason.");

        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Refunded)
            return new(true, false, order.StripeRefundStatus ?? "succeeded");
        if (order.Status != OrderStatus.Paid)
            throw new InvalidOperationException("Only a paid order can be refunded.");
        if (!IsConfigured) throw new InvalidOperationException("Stripe payments are not configured.");
        if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            throw new InvalidOperationException("This order is missing its Stripe payment reference.");

        Stripe.Refund refund;
        if (!string.IsNullOrWhiteSpace(order.StripeRefundId))
        {
            refund = await Refunds().GetAsync(order.StripeRefundId, cancellationToken: cancellationToken);
        }
        else
        {
            var amount = order.PaymentReceivedCents ?? order.TotalCents;
            if (amount is null or <= 0) throw new InvalidOperationException("The paid amount is unavailable.");
            refund = await Refunds().CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = order.StripePaymentIntentId,
                Amount = amount,
                Reason = reason,
                Metadata = new Dictionary<string, string>
                {
                    ["order_reference"] = order.Reference,
                    ["refund_scope"] = "full"
                }
            }, new RequestOptions { IdempotencyKey = $"craftisma-refund-{order.Reference}" }, cancellationToken);
        }

        await ApplyRefundAsync(order, refund.Id, refund.Status, refund.Amount, reason, cancellationToken);
        var complete = order.Status == OrderStatus.Refunded;
        return new(complete, !complete && string.Equals(order.StripeRefundStatus, "pending", StringComparison.OrdinalIgnoreCase),
            order.StripeRefundStatus ?? "unknown");
    }

    public async Task MarkRefundedAsync(Charge charge, CancellationToken cancellationToken = default)
    {
        if (!charge.Refunded || string.IsNullOrWhiteSpace(charge.PaymentIntentId)) return;
        var order = await db.Orders.SingleOrDefaultAsync(
            item => item.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);
        if (order is null || order.Status is OrderStatus.Cancelled or OrderStatus.PendingPayment) return;

        var latestRefund = charge.Refunds?.Data.OrderByDescending(item => item.Created).FirstOrDefault();
        await ApplyRefundAsync(
            order,
            latestRefund?.Id ?? order.StripeRefundId ?? $"charge-{charge.Id}",
            "succeeded",
            charge.AmountRefunded,
            latestRefund?.Reason ?? order.RefundReason ?? "requested_by_customer",
            cancellationToken);
    }

    private async Task ApplyRefundAsync(
        StoreOrder order,
        string refundId,
        string? refundStatus,
        long refundedCents,
        string reason,
        CancellationToken cancellationToken)
    {
        var alreadyComplete = order.Status == OrderStatus.Refunded;
        order.StripeRefundId = refundId;
        order.StripeRefundStatus = refundStatus ?? "unknown";
        order.RefundReason = reason;
        order.RefundRequestedUtc ??= DateTime.UtcNow;
        order.RefundedCents = refundedCents;

        var paidCents = order.PaymentReceivedCents ?? order.TotalCents;
        var isComplete = string.Equals(refundStatus, "succeeded", StringComparison.OrdinalIgnoreCase) &&
            paidCents is > 0 && refundedCents >= paidCents;
        if (!isComplete)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        order.Status = OrderStatus.Refunded;
        order.RefundedUtc ??= DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (!alreadyComplete) await notifications.QueueRefundAsync(order.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
