using System.ComponentModel.DataAnnotations;
using KashefProject.Data;

namespace KashefProject.Models;

public sealed class CustomerRegisterViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Full name")]
    public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the Terms and Privacy Policy.")]
    public bool AcceptPolicies { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class CustomerLoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Display(Name = "Keep me signed in")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class AccountEmailViewModel
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
}

public sealed class ResetPasswordViewModel
{
    [Required] public string UserId { get; set; } = "";
    [Required] public string Code { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class CustomerProfileViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Full name")]
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}

public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password), Display(Name = "New password")]
    public string NewPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed record CustomerDashboardViewModel(
    string FullName,
    string Email,
    IReadOnlyList<StoreOrder> Orders,
    int PaidOrderCount,
    long LifetimeSpendCents);
