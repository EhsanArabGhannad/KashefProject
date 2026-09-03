using System.Globalization;
using KashefProject.Data;
using KashefProject.Models;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync();
    Task<IReadOnlyList<Product>> GetProductsAsync();
    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync();
    Task<Product?> FindProductAsync(string slug);
    Task<CategoryPageViewModel?> FindCategoryAsync(string slug);
}

public sealed class CatalogService(StoreDbContext db) : ICatalogService
{
    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync() =>
        (await db.Categories.AsNoTracking()
            .Where(category => category.IsPublished)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync())
        .Select(MapCategory)
        .ToArray();

    public async Task<IReadOnlyList<Product>> GetProductsAsync() =>
        (await ProductQuery()
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToListAsync())
        .Select(MapProduct)
        .ToArray();

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync() =>
        (await ProductQuery()
            .Where(product => product.IsFeatured)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToListAsync())
        .Select(MapProduct)
        .ToArray();

    public async Task<Product?> FindProductAsync(string slug)
    {
        var product = await ProductQuery()
            .FirstOrDefaultAsync(product => product.Slug == slug);
        return product is null ? null : MapProduct(product);
    }

    public async Task<CategoryPageViewModel?> FindCategoryAsync(string slug)
    {
        var category = await db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(category => category.IsPublished && category.Slug == slug);
        if (category is null)
        {
            return null;
        }

        var products = await ProductQuery()
            .Where(product => product.CategoryId == category.Id)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToListAsync();

        return new CategoryPageViewModel(MapCategory(category), products.Select(MapProduct).ToArray());
    }

    private IQueryable<CatalogProduct> ProductQuery() => db.Products.AsNoTracking()
        .Where(product => product.IsPublished)
        .Include(product => product.Category)
        .Include(product => product.Images.OrderBy(image => image.SortOrder));

    private static ProductCategory MapCategory(CatalogCategory category) => new(
        category.Slug,
        category.Name,
        category.Kicker,
        category.Description,
        category.ImagePath,
        category.ImageAlt,
        category.CardClass);

    private static Product MapProduct(CatalogProduct product)
    {
        var images = product.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => image.ImagePath)
            .ToArray();
        var imageAlt = product.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.AltText
            ?? $"{product.Name} dimensional wall art";
        var price = product.PriceCents is null
            ? "Price on request"
            : (product.PriceCents.Value / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"));

        return new Product(
            product.Slug,
            product.Name,
            product.Category.Slug,
            product.Category.Name,
            price,
            product.ShortDescription,
            product.Description,
            product.Size,
            product.Material,
            product.Finish,
            images,
            imageAlt,
            product.Badge,
            product.CardClass,
            product.HighlightsText
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
