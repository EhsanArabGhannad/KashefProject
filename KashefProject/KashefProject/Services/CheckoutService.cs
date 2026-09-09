using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KashefProject.Data;
using KashefProject.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Services;

public sealed record CheckoutReview(int CartId, Guid Revision, string Fingerprint, DateTime ExpiresUtc);
public sealed record OrderSubmission(StoreOrder? Order, string? Error);

public sealed class CheckoutService(StoreDbContext db, ShoppingService shopping, CartOwner owner, IDataProtectionProvider protection)
{
    private readonly IDataProtector protector = protection.CreateProtector("Craftisma.CheckoutReview.v1");

    private static string Fingerprint(CartSummary summary) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(summary.Lines))));

    public async Task<CheckoutViewModel?> PrepareAsync(CheckoutViewModel? model = null)
    {
        var cart = await shopping.FindCartAsync();
        var summary = await shopping.SummarizeAsync(cart);
        if (cart is null || !summary.CanCheckout) return null;
        model ??= new CheckoutViewModel();
        model.Summary = summary;
        model.ReviewToken = protector.Protect(JsonSerializer.Serialize(new CheckoutReview(
            cart.Id, cart.Revision, Fingerprint(summary), DateTime.UtcNow.AddMinutes(30))));
        return model;
    }

    public async Task<OrderSubmission> SubmitAsync(CheckoutViewModel model)
    {
        CheckoutReview? review;
        try { review = JsonSerializer.Deserialize<CheckoutReview>(protector.Unprotect(model.ReviewToken)); }
        catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException)
        { return new(null, "This review is invalid. Please review the order again."); }
        var hash = owner.GetHash();
        if (review is null || hash is null) return new(null, "Please return to your bag and review the order again.");

        // SQLite serializes this read/validate/write transaction. A cart revision
        // can produce at most one order, even with concurrent double submissions.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var existing = await db.Orders.Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.CheckoutKey == review.Revision && order.OwnerHash == hash);
        if (existing is not null) return new(existing, null);
        if (review.ExpiresUtc <= DateTime.UtcNow) return new(null, "Your review expired. Please check the current prices and try again.");
        var cart = await shopping.FindCartAsync();
        var summary = await shopping.SummarizeAsync(cart);
        if (cart is null || cart.Id != review.CartId || cart.Revision != review.Revision ||
            !summary.CanCheckout || Fingerprint(summary) != review.Fingerprint)
            return new(null, "Your bag or product details have changed. Please review the updated order before saving.");

        var order = new StoreOrder
        {
            Reference = "CF-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            OwnerHash = hash, CheckoutKey = review.Revision,
            FullName = model.FullName.Trim(), Email = model.Email.Trim(), Phone = model.Phone?.Trim(),
            AddressLine1 = model.AddressLine1.Trim(), AddressLine2 = model.AddressLine2?.Trim(),
            City = model.City.Trim(), State = model.State, PostalCode = model.PostalCode.Trim(),
            SubtotalCents = summary.SubtotalCents,
            Lines = summary.Lines.Select(line => new StoreOrderLine
            {
                ProductId = line.ProductId, ProductName = line.Name, Finish = line.Finish, Size = line.Size,
                Quantity = line.Quantity, UnitPriceCents = line.UnitPriceCents!.Value
            }).ToList()
        };
        db.Orders.Add(order);
        db.ShoppingCartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.Revision = Guid.NewGuid();
        cart.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(order, null);
    }
}
