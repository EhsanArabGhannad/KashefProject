using System.ComponentModel.DataAnnotations;

namespace KashefProject.Models;

public sealed class CheckoutViewModel
{
    [Required, StringLength(4096)] public string ReviewToken { get; set; } = "";
    [Required, StringLength(120), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm that you reviewed your order and delivery details.")]
    public bool AcknowledgePending { get; set; }
    public CartSummary Summary { get; set; } = new([]);
    public long ShippingCents { get; set; }
    public bool AutomaticTaxEnabled { get; set; }
    public long TotalBeforeTaxCents => Summary.SubtotalCents + ShippingCents;
}
