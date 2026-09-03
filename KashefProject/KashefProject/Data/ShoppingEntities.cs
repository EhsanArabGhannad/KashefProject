using System.ComponentModel.DataAnnotations;

namespace KashefProject.Data;

public sealed class ShoppingCart
{
    public int Id { get; set; }
    [MaxLength(64)] public required string OwnerHash { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<ShoppingCartItem> Items { get; set; } = [];
}

public sealed class ShoppingCartItem
{
    public int Id { get; set; }
    public int ShoppingCartId { get; set; }
    public ShoppingCart ShoppingCart { get; set; } = null!;
    // Deliberately not a foreign key: deleting a product must not delete an order
    // or silently turn an unavailable cart item into an empty bag.
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
