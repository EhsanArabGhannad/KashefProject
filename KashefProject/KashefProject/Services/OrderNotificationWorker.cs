using KashefProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KashefProject.Services;

public sealed class OrderNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailDeliveryOptions> configuredOptions,
    IConfiguration configuration,
    ILogger<OrderNotificationWorker> logger) : BackgroundService
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
                logger.LogError(exception, "The order email worker encountered an unexpected error.");
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
        await db.OrderNotifications
            .Where(notification => notification.Status == OrderNotificationStatus.Sending &&
                notification.LastAttemptUtc < now.AddMinutes(-10))
            .ExecuteUpdateAsync(update => update
                .SetProperty(notification => notification.Status, OrderNotificationStatus.Pending)
                .SetProperty(notification => notification.NextAttemptUtc, now), cancellationToken);

        var notification = await db.OrderNotifications
            .Include(item => item.StoreOrder)
            .ThenInclude(order => order.Lines)
            .Where(item => item.Status == OrderNotificationStatus.Pending && item.NextAttemptUtc <= now)
            .OrderBy(item => item.NextAttemptUtc)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (notification is null) return false;

        notification.Status = OrderNotificationStatus.Sending;
        notification.LastAttemptUtc = now;
        notification.AttemptCount++;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var publicUrl = configuration["Site:PublicUrl"]?.TrimEnd('/') ?? "https://craftisma.net";
            var email = OrderEmailComposer.Compose(notification.Kind, notification.StoreOrder, options, publicUrl);
            notification.ProviderMessageId = await sender.SendAsync(email, cancellationToken);
            notification.Status = OrderNotificationStatus.Sent;
            notification.SentUtc = DateTime.UtcNow;
            notification.LastError = null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            notification.Status = notification.AttemptCount >= 8
                ? OrderNotificationStatus.Failed
                : OrderNotificationStatus.Pending;
            notification.NextAttemptUtc = DateTime.UtcNow.AddMinutes(Math.Min(360, Math.Pow(2, notification.AttemptCount)));
            notification.LastError = exception.Message.Length <= 500 ? exception.Message : exception.Message[..500];
            logger.LogWarning(exception, "Could not deliver order notification {NotificationId}; attempt {AttemptCount}.",
                notification.Id, notification.AttemptCount);
        }
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
