using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using KashefProject.Data;
using KashefProject.Models;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Services;

public sealed class CartOwner(IHttpContextAccessor accessor, IWebHostEnvironment environment)
{
    private string? ownerHash;
    public string? GetHash(bool create = false)
    {
        if (ownerHash is not null) return ownerHash;
        var context = accessor.HttpContext!;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            ownerHash = Hash($"user:{userId}");
            return ownerHash;
        }
        return GetGuestHash(create);
    }

    public string? GetGuestHash(bool create = false)
    {
        var context = accessor.HttpContext!;
        var cookieName = environment.IsDevelopment() ? "Craftisma.Bag" : "__Host-Craftisma.Bag";
        var token = context.Request.Cookies[cookieName];
        if (token is null || token.Length != 64 || !token.All(Uri.IsHexDigit))
        {
            if (!create) return null;
            token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            context.Response.Cookies.Append(cookieName, token, new CookieOptions
            {
                HttpOnly = true, Secure = !environment.IsDevelopment(), SameSite = SameSiteMode.Lax,
                Path = "/", MaxAge = TimeSpan.FromDays(30), IsEssential = true
            });
        }
        // Keep the original guest-cart hash format so bags created before
        // customer accounts were introduced continue to work after deployment.
        return Hash(token);
    }

    public static string ForUser(string userId) => Hash($"user:{userId}");

    public void ClearGuestCookie()
    {
        var cookieName = environment.IsDevelopment() ? "Craftisma.Bag" : "__Host-Craftisma.Bag";
        accessor.HttpContext!.Response.Cookies.Delete(cookieName, new CookieOptions
        {
            Secure = !environment.IsDevelopment(), SameSite = SameSiteMode.Lax, Path = "/"
        });
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class ShoppingService(StoreDbContext db, CartOwner owner)
{
    public const int MaxQuantity = 20;
    public const int MaxLines = 20;

    public Task<ShoppingCart?> FindCartAsync() => db.ShoppingCarts.Include(cart => cart.Items)
        .SingleOrDefaultAsync(cart => cart.OwnerHash == owner.GetHash(false));

    public async Task<CartSummary> GetSummaryAsync() => await SummarizeAsync(await FindCartAsync());

    public async Task<CartSummary> SummarizeAsync(ShoppingCart? cart)
    {
        if (cart is null) return new CartSummary([]);
        var ids = cart.Items.Select(item => item.ProductId).ToArray();
        var products = await db.Products.AsNoTracking().Include(product => product.Category)
            .Include(product => product.Images).Where(product => ids.Contains(product.Id)).ToDictionaryAsync(product => product.Id);
        return new CartSummary(cart.Items.OrderBy(item => item.Id).Select(item =>
        {
            products.TryGetValue(item.ProductId, out var product);
            var available = product is { IsPublished: true, PriceCents: > 0 } && product.Category.IsPublished;
            return new CartLine(item.ProductId, product?.Name ?? "Unavailable product", product?.Slug ?? "",
                product?.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.ImagePath,
                product?.Finish ?? "", product?.Size ?? "", item.Quantity, product?.PriceCents, available);
        }).ToArray());
    }

    public async Task<string?> AddAsync(int productId, int quantity)
    {
        if (quantity is < 1 or > MaxQuantity) return "Choose a quantity between 1 and 20.";
        await using var transaction = await db.Database.BeginTransactionAsync();
        var product = await db.Products.Include(item => item.Category).SingleOrDefaultAsync(item => item.Id == productId);
        if (product is not { IsPublished: true, PriceCents: > 0 } || !product.Category.IsPublished)
            return "This piece is not available to order. Please contact us for pricing.";

        var hash = owner.GetHash(true)!;
        var cart = await db.ShoppingCarts.Include(item => item.Items).SingleOrDefaultAsync(item => item.OwnerHash == hash);
        if (cart is null)
        {
            cart = new ShoppingCart { OwnerHash = hash };
            db.ShoppingCarts.Add(cart);
        }
        var line = cart.Items.SingleOrDefault(item => item.ProductId == productId);
        if (line is not null && line.Quantity + quantity > MaxQuantity) return "You can add up to 20 of each piece.";
        if (line is null && cart.Items.Count >= MaxLines) return "Your bag can hold up to 20 different pieces.";
        if (line is null) cart.Items.Add(new ShoppingCartItem { ProductId = productId, Quantity = quantity });
        else line.Quantity += quantity;
        cart.Revision = Guid.NewGuid();
        cart.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task<string?> UpdateAsync(int productId, int quantity)
    {
        if (quantity is < 0 or > MaxQuantity) return "Choose a quantity between 1 and 20, or remove the piece.";
        await using var transaction = await db.Database.BeginTransactionAsync();
        var cart = await FindCartAsync();
        var line = cart?.Items.SingleOrDefault(item => item.ProductId == productId);
        if (cart is null || line is null) return "That piece is no longer in your bag.";
        if (quantity == 0) db.ShoppingCartItems.Remove(line);
        else line.Quantity = quantity;
        cart.Revision = Guid.NewGuid();
        cart.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task MergeGuestCartAsync(string? guestHash, string userId)
    {
        if (string.IsNullOrWhiteSpace(guestHash)) return;
        var userHash = CartOwner.ForUser(userId);
        if (guestHash == userHash) return;

        await using var transaction = await db.Database.BeginTransactionAsync();
        var guest = await db.ShoppingCarts.Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.OwnerHash == guestHash);
        if (guest is null) return;
        var account = await db.ShoppingCarts.Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.OwnerHash == userHash);
        if (account is null)
        {
            guest.OwnerHash = userHash;
            guest.Revision = Guid.NewGuid();
            guest.UpdatedUtc = DateTime.UtcNow;
        }
        else
        {
            foreach (var guestLine in guest.Items)
            {
                var accountLine = account.Items.SingleOrDefault(item => item.ProductId == guestLine.ProductId);
                if (accountLine is not null)
                    accountLine.Quantity = Math.Min(MaxQuantity, accountLine.Quantity + guestLine.Quantity);
                else if (account.Items.Count < MaxLines)
                    account.Items.Add(new ShoppingCartItem { ProductId = guestLine.ProductId, Quantity = guestLine.Quantity });
            }
            account.Revision = Guid.NewGuid();
            account.UpdatedUtc = DateTime.UtcNow;
            db.ShoppingCarts.Remove(guest);
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
