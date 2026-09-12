using System.ComponentModel.DataAnnotations;

namespace KashefProject.Models;

public sealed class ContactInquiryViewModel
{
    public static readonly IReadOnlyList<string> Interests =
    [
        "Custom color or size",
        "Original custom piece",
        "Product question",
        "Wholesale inquiry"
    ];

    [Required, StringLength(120, MinimumLength = 2)]
    [Display(Name = "Your name")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(80)]
    [Display(Name = "What are you interested in?")]
    public string Interest { get; set; } = Interests[0];

    [Required, StringLength(4000, MinimumLength = 20)]
    [Display(Name = "Tell us about your idea")]
    public string Message { get; set; } = string.Empty;

    // Honeypot: real visitors never see or fill this field.
    public string? Website { get; set; }
}
