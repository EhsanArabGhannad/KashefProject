using System.Text.Encodings.Web;
using KashefProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KashefProject.Services;

public sealed class ContactInquiryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailDeliveryOptions> configuredOptions,
    IConfiguration configuration,
    ILogger<ContactInquiryWorker> logger) : BackgroundService
{
    private readonly EmailDeliveryOptions options = configuredOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!options.IsConfigured)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The contact inquiry worker encountered an unexpected error.");
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ITransactionalEmailSender>();
        if (!sender.IsConfigured) return false;

        var now = DateTime.UtcNow;
        await db.ContactInquiries
            .Where(inquiry => inquiry.NotificationStatus == ContactInquiryNotificationStatus.Sending &&
                inquiry.LastAttemptUtc < now.AddMinutes(-10))
            .ExecuteUpdateAsync(update => update
                .SetProperty(inquiry => inquiry.NotificationStatus, ContactInquiryNotificationStatus.Pending)
                .SetProperty(inquiry => inquiry.NextAttemptUtc, now), cancellationToken);

        var inquiry = await db.ContactInquiries
            .Where(item => item.NotificationStatus == ContactInquiryNotificationStatus.Pending && item.NextAttemptUtc <= now)
            .OrderBy(item => item.NextAttemptUtc)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (inquiry is null) return false;

        inquiry.NotificationStatus = ContactInquiryNotificationStatus.Sending;
        inquiry.LastAttemptUtc = now;
        inquiry.AttemptCount++;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var publicUrl = configuration["Site:PublicUrl"]?.TrimEnd('/') ?? "https://craftisma.net";
            var encodedName = HtmlEncoder.Default.Encode(inquiry.Name);
            var encodedEmail = HtmlEncoder.Default.Encode(inquiry.Email);
            var encodedInterest = HtmlEncoder.Default.Encode(inquiry.Interest);
            var encodedMessage = HtmlEncoder.Default.Encode(inquiry.Message).Replace("\r\n", "<br>").Replace("\n", "<br>");
            var email = new TransactionalEmail(
                options.AdminAddress,
                $"New Craftisma inquiry {inquiry.Reference} — {inquiry.Interest}",
                $"<!doctype html><html><body style=\"margin:0;background:#f4f1ea;color:#171716;font-family:Arial,sans-serif\"><div style=\"max-width:620px;margin:0 auto;padding:40px 22px\"><p style=\"font-size:22px;font-weight:800\">CRAFTISMA<span style=\"color:#ff6b35\">.</span></p><div style=\"padding:28px;background:#fff;border:1px solid #d7d1c5;border-radius:20px;line-height:1.65\"><p style=\"color:#ff6b35;font-weight:700\">{encodedInterest}</p><h1 style=\"font-size:26px\">New inquiry from {encodedName}</h1><p><a href=\"mailto:{encodedEmail}\">{encodedEmail}</a></p><p>{encodedMessage}</p><p><a href=\"{publicUrl}/admin/inquiries/{inquiry.Id}\" style=\"color:#171716;font-weight:700\">Open in Craftisma Admin →</a></p></div></div></body></html>",
                $"New Craftisma inquiry {inquiry.Reference}\n\nFrom: {inquiry.Name} <{inquiry.Email}>\nInterest: {inquiry.Interest}\n\n{inquiry.Message}\n\n{publicUrl}/admin/inquiries/{inquiry.Id}",
                $"craftisma_inquiry_{inquiry.Reference}",
                inquiry.Email);
            inquiry.ProviderMessageId = await sender.SendAsync(email, cancellationToken);
            inquiry.NotificationStatus = ContactInquiryNotificationStatus.Sent;
            inquiry.SentUtc = DateTime.UtcNow;
            inquiry.LastError = null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            inquiry.NotificationStatus = inquiry.AttemptCount >= 8
                ? ContactInquiryNotificationStatus.Failed
                : ContactInquiryNotificationStatus.Pending;
            inquiry.NextAttemptUtc = DateTime.UtcNow.AddMinutes(Math.Min(360, Math.Pow(2, inquiry.AttemptCount)));
            inquiry.LastError = exception.Message.Length <= 500 ? exception.Message : exception.Message[..500];
            logger.LogWarning(exception, "Could not deliver contact inquiry {InquiryId}; attempt {AttemptCount}.",
                inquiry.Id, inquiry.AttemptCount);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
