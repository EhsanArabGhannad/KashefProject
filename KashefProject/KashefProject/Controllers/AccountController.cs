using System.Security.Claims;
using System.Text;
using KashefProject.Data;
using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Controllers;

[Route("account")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    StoreDbContext db,
    ShoppingService shopping,
    CartOwner cartOwner,
    AccountEmailService emailService,
    IConfiguration configuration) : Controller
{
    [AllowAnonymous, HttpGet("register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        return View(new CustomerRegisterViewModel { ReturnUrl = SafeReturnUrl(returnUrl) });
    }

    [AllowAnonymous, HttpPost("register"), ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> Register(CustomerRegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        model.Email = model.Email.Trim();
        model.FullName = model.FullName.Trim();
        model.ReturnUrl = SafeReturnUrl(model.ReturnUrl);
        if (!ModelState.IsValid) return View(model);

        var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = false };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddIdentityErrors(roleResult);
            return View(model);
        }

        db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id, FullName = model.FullName });
        await db.SaveChangesAsync();
        await SendConfirmationAsync(user, model.FullName, model.ReturnUrl);
        return RedirectToAction(nameof(CheckEmail), new { email = model.Email });
    }

    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        return View(new CustomerLoginViewModel { ReturnUrl = SafeReturnUrl(returnUrl) });
    }

    [AllowAnonymous, HttpPost("login"), ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> Login(CustomerLoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        model.Email = model.Email.Trim();
        model.ReturnUrl = SafeReturnUrl(model.ReturnUrl);
        if (!ModelState.IsValid) return View(model);

        var guestHash = cartOwner.GetGuestHash();
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "Too many attempts. Please try again in 15 minutes."
                : result.IsNotAllowed && !user.EmailConfirmed
                    ? "Please confirm your email before signing in."
                    : "The email or password is incorrect.");
            return View(model);
        }

        await shopping.MergeGuestCartAsync(guestHash, user.Id);
        cartOwner.ClearGuestCookie();
        return RedirectAfterAccountAction(model.ReturnUrl);
    }

    [Authorize, HttpPost("logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous, HttpGet("check-email")]
    public IActionResult CheckEmail(string? email = null)
    {
        ViewData["Email"] = email;
        return View();
    }

    [AllowAnonymous, HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)) return View("ConfirmationResult", false);
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return View("ConfirmationResult", false);
        IdentityResult result;
        try
        {
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            result = user.EmailConfirmed ? IdentityResult.Success : await userManager.ConfirmEmailAsync(user, token);
        }
        catch (FormatException)
        {
            result = IdentityResult.Failed(new IdentityError());
        }

        if (result.Succeeded)
        {
            var guestHash = cartOwner.GetGuestHash();
            await signInManager.SignInAsync(user, isPersistent: false);
            await shopping.MergeGuestCartAsync(guestHash, user.Id);
            cartOwner.ClearGuestCookie();
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
        }
        return View("ConfirmationResult", result.Succeeded);
    }

    [AllowAnonymous, HttpGet("resend-confirmation")]
    public IActionResult ResendConfirmation() => View(new AccountEmailViewModel());

    [AllowAnonymous, HttpPost("resend-confirmation"), ValidateAntiForgeryToken, EnableRateLimiting("recovery")]
    public async Task<IActionResult> ResendConfirmation(AccountEmailViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await userManager.FindByEmailAsync(model.Email.Trim());
            if (user is { EmailConfirmed: false })
            {
                var name = await ProfileNameAsync(user);
                await SendConfirmationAsync(user, name, null);
            }
        }
        return RedirectToAction(nameof(CheckEmail), new { email = model.Email.Trim() });
    }

    [AllowAnonymous, HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new AccountEmailViewModel());

    [AllowAnonymous, HttpPost("forgot-password"), ValidateAntiForgeryToken, EnableRateLimiting("recovery")]
    public async Task<IActionResult> ForgotPassword(AccountEmailViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await userManager.FindByEmailAsync(model.Email.Trim());
            if (user is { EmailConfirmed: true, Email: not null })
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = AbsoluteUrl("/account/reset-password", new { userId = user.Id, code });
                await emailService.SendPasswordResetAsync(user.Email, await ProfileNameAsync(user), link);
            }
        }
        return RedirectToAction(nameof(ForgotPasswordSent));
    }

    [AllowAnonymous, HttpGet("forgot-password-sent")]
    public IActionResult ForgotPasswordSent() => View();

    [AllowAnonymous, HttpGet("reset-password")]
    public IActionResult ResetPassword(string? userId, string? code) =>
        string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)
            ? BadRequest()
            : View(new ResetPasswordViewModel { UserId = userId, Code = code });

    [AllowAnonymous, HttpPost("reset-password"), ValidateAntiForgeryToken, EnableRateLimiting("recovery")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null) return RedirectToAction(nameof(ResetPasswordConfirmation));
        IdentityResult result;
        try
        {
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            result = await userManager.ResetPasswordAsync(user, token, model.Password);
        }
        catch (FormatException)
        {
            result = IdentityResult.Failed(new IdentityError { Description = "This reset link is invalid or has expired." });
        }
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }
        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [AllowAnonymous, HttpGet("reset-password-complete")]
    public IActionResult ResetPasswordConfirmation() => View();

    [Authorize, HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();
        var profile = await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId);
        var orders = await db.Orders.AsNoTracking().Include(item => item.Lines)
            .Where(item => item.CustomerUserId == userId).OrderByDescending(item => item.CreatedUtc).ToListAsync();
        return View(new CustomerDashboardViewModel(
            profile?.FullName ?? user.Email?.Split('@')[0] ?? "Customer",
            user.Email ?? "",
            orders,
            orders.Count(item => item.Status == OrderStatus.Paid),
            orders.Where(item => item.Status == OrderStatus.Paid).Sum(item => item.PaymentReceivedCents ?? item.TotalCents ?? 0)));
    }

    [Authorize, HttpGet("orders/{reference}")]
    public async Task<IActionResult> Order(string reference)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.CustomerUserId == userId);
        return order is null ? NotFound() : View(order);
    }

    [Authorize, HttpGet("orders/{reference}/invoice")]
    public async Task<IActionResult> Invoice(string reference)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await db.Orders.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Reference == reference && item.CustomerUserId == userId && item.Status == OrderStatus.Paid);
        return order is null ? NotFound() : View(order);
    }

    [Authorize, HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();
        return View(new CustomerProfileViewModel { FullName = await ProfileNameAsync(user), Email = user.Email ?? "" });
    }

    [Authorize, HttpPost("profile"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(CustomerProfileViewModel model)
    {
        ModelState.Remove(nameof(model.Email));
        model.FullName = model.FullName.Trim();
        if (!ModelState.IsValid)
        {
            model.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
            return View(model);
        }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await db.CustomerProfiles.SingleOrDefaultAsync(item => item.UserId == userId);
        if (profile is null) db.CustomerProfiles.Add(new CustomerProfile { UserId = userId, FullName = model.FullName });
        else { profile.FullName = model.FullName; profile.UpdatedUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        TempData["AccountMessage"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet("change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost("change-password"), ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }
        await signInManager.RefreshSignInAsync(user);
        TempData["AccountMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SendConfirmationAsync(IdentityUser user, string fullName, string? returnUrl)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = AbsoluteUrl("/account/confirm-email", new { userId = user.Id, code, returnUrl = SafeReturnUrl(returnUrl) });
        await emailService.SendConfirmationAsync(user.Email!, fullName, link);
    }

    private async Task<string> ProfileNameAsync(IdentityUser user) =>
        (await db.CustomerProfiles.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == user.Id))?.FullName
        ?? user.Email?.Split('@')[0] ?? "there";

    private string AbsoluteUrl(string path, object values)
    {
        var site = configuration["Site:PublicUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(site)) site = $"{Request.Scheme}://{Request.Host}";
        return site + QueryHelpers.AddQueryString(path, values.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(values)?.ToString()));
    }

    private string? SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private IActionResult RedirectAfterAccountAction(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }
}
