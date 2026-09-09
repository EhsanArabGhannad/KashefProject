using System.ComponentModel.DataAnnotations;

namespace KashefProject.Data;

public enum OrderStatus { PendingPayment, Paid, Cancelled }

public sealed class StoreOrder
{
    public int Id { get; set; }
    [MaxLength(32)] public required string Reference { get; set; }
    [MaxLength(64)] public required string OwnerHash { get; set; }
    public Guid CheckoutKey { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    [MaxLength(120)] public required string FullName { get; set; }
    [MaxLength(254)] public required string Email { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(160)] public required string AddressLine1 { get; set; }
    [MaxLength(160)] public string? AddressLine2 { get; set; }
    [MaxLength(80)] public required string City { get; set; }
    [MaxLength(2)] public required string State { get; set; }
    [MaxLength(10)] public required string PostalCode { get; set; }
    [MaxLength(2)] public string Country { get; set; } = "US";
    [MaxLength(3)] public string Currency { get; set; } = "USD";
    public long SubtotalCents { get; set; }
    // Null means not calculated. Never represent unknown shipping/tax as free.
    public long? ShippingCents { get; set; }
    public long? TaxCents { get; set; }
    public long? TotalCents { get; set; }
    [MaxLength(255)] public string? StripeCheckoutSessionId { get; set; }
    [MaxLength(255)] public string? StripePaymentIntentId { get; set; }
    public long? PaymentReceivedCents { get; set; }
    public DateTime? PaidUtc { get; set; }
    public List<StoreOrderLine> Lines { get; set; } = [];
}

public sealed class StoreOrderLine
{
    public int Id { get; set; }
    public int StoreOrderId { get; set; }
    public StoreOrder StoreOrder { get; set; } = null!;
    public int ProductId { get; set; }
    [MaxLength(160)] public required string ProductName { get; set; }
    [MaxLength(160)] public required string Finish { get; set; }
    [MaxLength(160)] public required string Size { get; set; }
    public long UnitPriceCents { get; set; }
    public int Quantity { get; set; }
}
