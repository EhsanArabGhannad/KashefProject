using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace KashefProject.Controllers;

[ApiController]
[Route("stripe/webhook")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StripeWebhookController(StripePaymentService payments, ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost, RequestSizeLimit(1024 * 1024)]
    public async Task<IActionResult> Receive()
    {
        if (!payments.IsWebhookConfigured) return NotFound();
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        try
        {
            var stripeEvent = payments.VerifyWebhook(json, Request.Headers["Stripe-Signature"].ToString());
            if (stripeEvent.Type is "checkout.session.completed" or "checkout.session.async_payment_succeeded" &&
                stripeEvent.Data.Object is Session session)
                await payments.MarkPaidAsync(session);
            else if (stripeEvent.Type == "charge.refunded" && stripeEvent.Data.Object is Charge charge)
                await payments.MarkRefundedAsync(charge);
            return Ok();
        }
        catch (Stripe.StripeException exception)
        {
            logger.LogWarning(exception, "Rejected an invalid Stripe webhook.");
            return BadRequest();
        }
    }
}
