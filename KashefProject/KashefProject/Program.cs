using KashefProject.Data;
using KashefProject.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<StoreDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/login";
    options.Cookie.Name = builder.Environment.IsDevelopment() ? "Craftisma.Auth" : "__Host-Craftisma.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        var loginPath = context.Request.Path.StartsWithSegments("/admin") ? "/admin/login" : "/account/login";
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        context.Response.Redirect(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(loginPath, "returnUrl", returnUrl));
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect(context.Request.Path.StartsWithSegments("/admin") ? "/admin/login" : "/account/");
        return Task.CompletedTask;
    };
});
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(24));
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartOwner>();
builder.Services.AddScoped<ShoppingService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<AccountEmailService>();
builder.Services.AddOptions<FulfillmentOptions>()
    .Bind(builder.Configuration.GetSection(FulfillmentOptions.SectionName))
    .Validate(options => options.OriginState == "VA", "The fulfillment origin must remain Virginia (VA).")
    .Validate(options => options.StandardShippingCents >= 0, "Standard shipping cannot be negative.")
    .Validate(options => options.FreeShippingThresholdCents > 0, "The free-shipping threshold must be positive.")
    .ValidateOnStart();
builder.Services.AddScoped<FulfillmentPolicy>();
builder.Services.Configure<StripePaymentOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<StripePaymentService>();
builder.Services.Configure<EmailDeliveryOptions>(builder.Configuration.GetSection(EmailDeliveryOptions.SectionName));
builder.Services.AddHttpClient<ITransactionalEmailSender, ResendEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<OrderNotificationQueue>();
builder.Services.AddHostedService<OrderNotificationWorker>();
builder.Services.AddHostedService<ContactInquiryWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsync("Too many requests. Please wait a few minutes and try again.", cancellationToken);
    };
    options.AddPolicy("cart", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("checkout", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("contact", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
    options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
    options.AddPolicy("recovery", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
});

var uploadRootSetting = builder.Configuration["Storage:UploadRoot"] ?? "App_Data/uploads";
var uploadRoot = Path.IsPathRooted(uploadRootSetting)
    ? uploadRootSetting
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, uploadRootSetting));
Directory.CreateDirectory(uploadRoot);
var keyRootSetting = builder.Configuration["Storage:DataProtectionRoot"] ?? "App_Data/keys";
var keyRoot = Path.IsPathRooted(keyRootSetting)
    ? keyRootSetting
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keyRootSetting));
Directory.CreateDirectory(keyRoot);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRoot))
    .SetApplicationName("Craftisma");
builder.Services.AddSingleton(new ImageStorageOptions(uploadRoot));
builder.Services.AddScoped<IImageStorage, LocalImageStorage>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads"
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration);

app.Run();
