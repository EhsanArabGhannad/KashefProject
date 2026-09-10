using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Text.Json;
using KashefProject.Data;
using KashefProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KashefProject.Services;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "Email";
    public string ApiKey { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Craftisma";
    public string AdminAddress { get; set; } = "";
    public bool IsConfigured =>
        ApiKey.StartsWith("re_", StringComparison.Ordinal) &&
        MailAddress.TryCreate(FromAddress, out _) &&
        MailAddress.TryCreate(AdminAddress, out _);
}

public sealed record TransactionalEmail(
    string To,
    string Subject,
    string Html,
    string Text,
    string IdempotencyKey);

public interface ITransactionalEmailSender
{
    bool IsConfigured { get; }
    Task<string?> SendAsync(TransactionalEmail email, CancellationToken cancellationToken);
}

public sealed class ResendEmailSender(HttpClient client, IOptions<EmailDeliveryOptions> configuredOptions)
    : ITransactionalEmailSender
{
    private readonly EmailDeliveryOptions options = configuredOptions.Value;

    public bool IsConfigured => options.IsConfigured;

    public async Task<string?> SendAsync(TransactionalEmail email, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("Transactional email is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Add("Idempotency-Key", email.IdempotencyKey);
        request.Content = JsonContent.Create(new
        {
            from = $"{options.FromName} <{options.FromAddress}>",
            to = new[] { email.To },
            subject = email.Subject,
            html = email.Html,
            text = email.Text
        });
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Email provider returned HTTP {(int)response.StatusCode}.");
        using var document = JsonDocument.Parse(responseBody);
        return document.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }
}

public static class OrderEmailComposer
{
    public static TransactionalEmail Compose(
        OrderNotificationKind kind,
        StoreOrder order,
        EmailDeliveryOptions options,
        string publicUrl)
    {
        var encodedReference = HtmlEncoder.Default.Encode(order.Reference);
        var encodedName = HtmlEncoder.Default.Encode(order.FullName);
        var total = StoreMoney.Format(order.PaymentReceivedCents ?? order.TotalCents ??
            order.SubtotalCents + (order.ShippingCents ?? 0));
        var itemRows = string.Join("", order.Lines.Select(line =>
            $"<tr><td style=\"padding:10px 0;border-bottom:1px solid #ddd7ca\"><strong>{HtmlEncoder.Default.Encode(line.ProductName)}</strong><br><span style=\"color:#726d64;font-size:13px\">{HtmlEncoder.Default.Encode(line.Size)} · {HtmlEncoder.Default.Encode(line.Finish)} · Qty {line.Quantity}</span></td><td style=\"padding:10px 0;border-bottom:1px solid #ddd7ca;text-align:right\">{StoreMoney.Format(line.UnitPriceCents * line.Quantity)}</td></tr>"));
        var textItems = string.Join(Environment.NewLine, order.Lines.Select(line =>
            $"- {line.ProductName} — {line.Size}, {line.Finish}, Qty {line.Quantity}: {StoreMoney.Format(line.UnitPriceCents * line.Quantity)}"));

        return kind switch
        {
            OrderNotificationKind.PaymentCustomer => Create(
                order.Email,
                $"Order {order.Reference} confirmed — Craftisma",
                $"<p>Hi {encodedName},</p><p>Your payment has been confirmed and we are preparing your Craftisma order.</p><table style=\"width:100%;border-collapse:collapse\">{itemRows}</table><p style=\"font-size:22px\"><strong>Total paid: {total}</strong></p><p>We will email your tracking number as soon as your order ships.</p>",
                $"Hi {order.FullName},{Environment.NewLine}{Environment.NewLine}Your payment has been confirmed for order {order.Reference}.{Environment.NewLine}{Environment.NewLine}{textItems}{Environment.NewLine}{Environment.NewLine}Total paid: {total}{Environment.NewLine}{Environment.NewLine}We will email your tracking number as soon as your order ships.",
                $"craftisma_payment_customer_{order.Reference}"),
            OrderNotificationKind.PaymentAdmin => Create(
                options.AdminAddress,
                $"New paid order {order.Reference} — {total}",
                $"<p>A new order has been paid and is ready to prepare.</p><p><strong>{encodedName}</strong><br>{HtmlEncoder.Default.Encode(order.Email)}</p><table style=\"width:100%;border-collapse:collapse\">{itemRows}</table><p style=\"font-size:22px\"><strong>Total paid: {total}</strong></p><p><a href=\"{publicUrl}/admin/orders/{order.Id}\" style=\"color:#171716;font-weight:700\">Open this order in Craftisma Admin →</a></p>",
                $"New paid order {order.Reference}{Environment.NewLine}{Environment.NewLine}Customer: {order.FullName} ({order.Email}){Environment.NewLine}{Environment.NewLine}{textItems}{Environment.NewLine}{Environment.NewLine}Total paid: {total}{Environment.NewLine}{publicUrl}/admin/orders/{order.Id}",
                $"craftisma_payment_admin_{order.Reference}"),
            OrderNotificationKind.ShipmentCustomer => Create(
                order.Email,
                $"Your Craftisma order {order.Reference} is on the way",
                $"<p>Hi {encodedName},</p><p>Your Craftisma order has shipped.</p><p style=\"padding:18px;background:#f4f1ea;border-radius:14px\"><strong>{HtmlEncoder.Default.Encode(order.TrackingCarrier ?? "Shipping carrier")}</strong><br>Tracking number: <strong>{HtmlEncoder.Default.Encode(order.TrackingNumber ?? "")}</strong></p><p>Please allow the carrier some time to activate tracking.</p>",
                $"Hi {order.FullName},{Environment.NewLine}{Environment.NewLine}Your Craftisma order {order.Reference} has shipped.{Environment.NewLine}Carrier: {order.TrackingCarrier}{Environment.NewLine}Tracking number: {order.TrackingNumber}{Environment.NewLine}{Environment.NewLine}Please allow the carrier some time to activate tracking.",
                $"craftisma_shipment_customer_{order.Reference}"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static TransactionalEmail Create(string to, string subject, string content, string text, string key) =>
        new(to, subject,
            $"<!doctype html><html><body style=\"margin:0;background:#f4f1ea;color:#171716;font-family:Arial,sans-serif\"><div style=\"max-width:620px;margin:0 auto;padding:40px 22px\"><p style=\"font-size:22px;font-weight:800;letter-spacing:-.5px\">CRAFTISMA<span style=\"color:#ff6b35\">.</span></p><div style=\"padding:28px;background:#fff;border:1px solid #d7d1c5;border-radius:20px;line-height:1.65\">{content}</div><p style=\"color:#726d64;font-size:12px;margin-top:20px\">Crafted objects and dimensional wall art · craftisma.net</p></div></body></html>",
            text,
            key);
}

public sealed class OrderNotificationQueue(StoreDbContext db)
{
    public Task QueuePaymentAsync(int orderId, CancellationToken cancellationToken = default) =>
        QueueAsync(orderId, [OrderNotificationKind.PaymentCustomer, OrderNotificationKind.PaymentAdmin], cancellationToken);

    public Task QueueShipmentAsync(int orderId, CancellationToken cancellationToken = default) =>
        QueueAsync(orderId, [OrderNotificationKind.ShipmentCustomer], cancellationToken);

    private async Task QueueAsync(int orderId, IReadOnlyList<OrderNotificationKind> kinds, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var kind in kinds)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT OR IGNORE INTO OrderNotifications
                    (StoreOrderId, Kind, Status, CreatedUtc, NextAttemptUtc, AttemptCount)
                VALUES
                    ({orderId}, {kind.ToString()}, {OrderNotificationStatus.Pending.ToString()}, {now}, {now}, {0})", cancellationToken);
        }
    }
}
