using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace KashefProject.Services;

public sealed class AccountEmailService(
    ITransactionalEmailSender sender,
    ILogger<AccountEmailService> logger)
{
    public bool IsConfigured => sender.IsConfigured;

    public Task<bool> SendConfirmationAsync(string email, string fullName, string link, CancellationToken cancellationToken = default) =>
        TrySendAsync(new TransactionalEmail(
            email,
            "Confirm your Craftisma account",
            Wrap($"<p>Hi {HtmlEncoder.Default.Encode(fullName)},</p><h1 style=\"font-size:28px\">Confirm your email</h1><p>Use the button below to verify your email address and finish setting up your Craftisma account.</p><p style=\"margin:28px 0\"><a href=\"{HtmlEncoder.Default.Encode(link)}\" style=\"display:inline-block;padding:14px 22px;border-radius:999px;background:#171716;color:#fff;text-decoration:none;font-weight:700\">Confirm email →</a></p><p style=\"font-size:13px;color:#726d64\">If you did not create this account, you can ignore this message.</p>"),
            $"Hi {fullName},\n\nConfirm your Craftisma account:\n{link}\n\nIf you did not create this account, you can ignore this message.",
            $"craftisma_confirm_{Digest(email + link)}"), cancellationToken);

    public Task<bool> SendPasswordResetAsync(string email, string fullName, string link, CancellationToken cancellationToken = default) =>
        TrySendAsync(new TransactionalEmail(
            email,
            "Reset your Craftisma password",
            Wrap($"<p>Hi {HtmlEncoder.Default.Encode(fullName)},</p><h1 style=\"font-size:28px\">Reset your password</h1><p>Use the button below to choose a new password. If you did not request this change, you can safely ignore this message.</p><p style=\"margin:28px 0\"><a href=\"{HtmlEncoder.Default.Encode(link)}\" style=\"display:inline-block;padding:14px 22px;border-radius:999px;background:#171716;color:#fff;text-decoration:none;font-weight:700\">Choose a new password →</a></p><p style=\"font-size:13px;color:#726d64\">For security, this link expires after a limited time and can be used only once.</p>"),
            $"Hi {fullName},\n\nReset your Craftisma password:\n{link}\n\nIf you did not request this change, you can ignore this message.",
            $"craftisma_reset_{Digest(email + link)}"), cancellationToken);

    private async Task<bool> TrySendAsync(TransactionalEmail email, CancellationToken cancellationToken)
    {
        if (!sender.IsConfigured) return false;
        try
        {
            await sender.SendAsync(email, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not send account email to the requested address.");
            return false;
        }
    }

    private static string Wrap(string content) =>
        $"<!doctype html><html><body style=\"margin:0;background:#f4f1ea;color:#171716;font-family:Arial,sans-serif\"><div style=\"max-width:620px;margin:0 auto;padding:40px 22px\"><p style=\"font-size:22px;font-weight:800\">CRAFTISMA<span style=\"color:#ff6b35\">.</span></p><div style=\"padding:28px;background:#fff;border:1px solid #d7d1c5;border-radius:20px;line-height:1.65\">{content}</div></div></body></html>";

    private static string Digest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..32];
}
