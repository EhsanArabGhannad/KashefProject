using KashefProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Categories.AnyAsync())
        {
            await SeedCatalogAsync(db);
        }

        await SeedAdministratorAsync(scope.ServiceProvider, configuration);
        await db.Database.ExecuteSqlRawAsync("PRAGMA optimize;");
    }

    private static async Task SeedCatalogAsync(StoreDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var categoryEntities = StoreCatalog.Categories
            .Select((category, index) => new CatalogCategory
            {
                Slug = category.Slug,
                Name = category.Name,
                Kicker = category.Kicker,
                Description = category.Description,
                ImagePath = category.ImagePath,
                ImageAlt = category.ImageAlt,
                CardClass = category.CardClass,
                DisplayOrder = index,
                IsPublished = true
            })
            .ToDictionary(category => category.Slug, StringComparer.OrdinalIgnoreCase);

        db.Categories.AddRange(categoryEntities.Values);
        await db.SaveChangesAsync();

        foreach (var (product, index) in StoreCatalog.Products.Select((item, index) => (item, index)))
        {
            var entity = new CatalogProduct
            {
                Slug = product.Slug,
                Name = product.Name,
                CategoryId = categoryEntities[product.CategorySlug].Id,
                PriceCents = null,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Size = product.Size,
                Material = product.Material,
                Finish = product.Finish,
                Badge = product.Badge,
                CardClass = product.CardClass,
                HighlightsText = string.Join(Environment.NewLine, product.Highlights),
                IsFeatured = product.Badge == "FEATURED",
                IsPublished = true,
                DisplayOrder = index,
                Images = product.Images.Select((path, imageIndex) => new CatalogProductImage
                {
                    ImagePath = path,
                    AltText = product.ImageAlt,
                    SortOrder = imageIndex
                }).ToList()
            };
            db.Products.Add(entity);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task SeedAdministratorAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Admin:Email"]?.Trim();
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
            EnsureSucceeded(roleResult, "creating the Admin role");
        }

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var userResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(userResult, "creating the administrator");
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Admin");
            EnsureSucceeded(roleResult, "assigning the Admin role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException($"Database initialization failed while {operation}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }
}
