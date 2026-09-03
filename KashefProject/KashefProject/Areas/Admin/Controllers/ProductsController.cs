using System.Text.RegularExpressions;
using KashefProject.Areas.Admin.Models;
using KashefProject.Data;
using KashefProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("admin/products")]
public sealed partial class ProductsController(StoreDbContext db, IImageStorage imageStorage) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await db.Products.AsNoTracking()
        .Include(product => product.Category)
        .Include(product => product.Images.OrderBy(image => image.SortOrder))
        .OrderBy(product => product.DisplayOrder)
        .ThenBy(product => product.Name)
        .ToListAsync());

    [HttpGet("create")]
    public async Task<IActionResult> Create() => View("Editor", await PrepareAsync(new ProductEditorViewModel()));

    [ValidateAntiForgeryToken]
    [HttpPost("create")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<IActionResult> Create(ProductEditorViewModel model, CancellationToken cancellationToken)
    {
        model.Slug = NormalizeSlug(model.Slug, model.Name);
        await ValidateEditorAsync(model, requireImage: true);
        if (!ModelState.IsValid)
        {
            return View("Editor", await PrepareAsync(model));
        }

        var product = new CatalogProduct
        {
            Slug = model.Slug,
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId,
            PriceCents = ToCents(model.PriceDollars),
            ShortDescription = model.ShortDescription.Trim(),
            Description = model.Description.Trim(),
            Size = model.Size.Trim(),
            Material = model.Material.Trim(),
            Finish = model.Finish.Trim(),
            Badge = model.Badge.Trim(),
            CardClass = model.CardClass,
            HighlightsText = NormalizeLines(model.HighlightsText),
            IsFeatured = model.IsFeatured,
            IsPublished = model.IsPublished,
            DisplayOrder = model.DisplayOrder
        };

        var savedPaths = new List<string>();
        try
        {
            foreach (var (file, index) in model.NewImages.Select((file, index) => (file, index)))
            {
                var path = await imageStorage.SaveAsync(file, cancellationToken);
                savedPaths.Add(path);
                product.Images.Add(new CatalogProductImage
                {
                    ImagePath = path,
                    AltText = $"{product.Name} wall art",
                    SortOrder = index
                });
            }

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            foreach (var path in savedPaths)
            {
                await imageStorage.DeleteAsync(path);
            }
            ModelState.AddModelError(nameof(model.NewImages), exception.Message);
            return View("Editor", await PrepareAsync(model));
        }

        TempData["StatusMessage"] = $"{product.Name} was added to the store.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products.AsNoTracking()
            .Include(item => item.Images.OrderBy(image => image.SortOrder))
            .FirstOrDefaultAsync(item => item.Id == id);
        return product is null ? NotFound() : View("Editor", await PrepareAsync(ToEditor(product)));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("{id:int}/edit")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, ProductEditorViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var product = await db.Products
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        model.Slug = NormalizeSlug(model.Slug, model.Name);
        var remainingImages = product.Images.Count(image => !model.RemoveImageIds.Contains(image.Id));
        await ValidateEditorAsync(model, requireImage: remainingImages == 0);
        if (!ModelState.IsValid)
        {
            model.ExistingImages = product.Images.OrderBy(image => image.SortOrder)
                .Select(image => new ExistingImageViewModel(image.Id, image.ImagePath, image.AltText)).ToList();
            return View("Editor", await PrepareAsync(model));
        }

        product.Slug = model.Slug;
        product.Name = model.Name.Trim();
        product.CategoryId = model.CategoryId;
        product.PriceCents = ToCents(model.PriceDollars);
        product.ShortDescription = model.ShortDescription.Trim();
        product.Description = model.Description.Trim();
        product.Size = model.Size.Trim();
        product.Material = model.Material.Trim();
        product.Finish = model.Finish.Trim();
        product.Badge = model.Badge.Trim();
        product.CardClass = model.CardClass;
        product.HighlightsText = NormalizeLines(model.HighlightsText);
        product.IsFeatured = model.IsFeatured;
        product.IsPublished = model.IsPublished;
        product.DisplayOrder = model.DisplayOrder;
        product.UpdatedUtc = DateTime.UtcNow;

        var imagesToRemove = product.Images.Where(image => model.RemoveImageIds.Contains(image.Id)).ToArray();
        db.ProductImages.RemoveRange(imagesToRemove);
        var nextSortOrder = product.Images.Except(imagesToRemove).Select(image => image.SortOrder).DefaultIfEmpty(-1).Max() + 1;
        var savedPaths = new List<string>();

        try
        {
            foreach (var file in model.NewImages)
            {
                var path = await imageStorage.SaveAsync(file, cancellationToken);
                savedPaths.Add(path);
                product.Images.Add(new CatalogProductImage
                {
                    ImagePath = path,
                    AltText = $"{product.Name} wall art",
                    SortOrder = nextSortOrder++
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            foreach (var image in imagesToRemove)
            {
                await imageStorage.DeleteAsync(image.ImagePath);
            }
        }
        catch (InvalidOperationException exception)
        {
            foreach (var path in savedPaths)
            {
                await imageStorage.DeleteAsync(path);
            }
            ModelState.AddModelError(nameof(model.NewImages), exception.Message);
            model.ExistingImages = product.Images.OrderBy(image => image.SortOrder)
                .Select(image => new ExistingImageViewModel(image.Id, image.ImagePath, image.AltText)).ToList();
            return View("Editor", await PrepareAsync(model));
        }

        TempData["StatusMessage"] = $"{product.Name} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.Include(item => item.Images).FirstOrDefaultAsync(item => item.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        foreach (var image in product.Images)
        {
            await imageStorage.DeleteAsync(image.ImagePath);
        }

        TempData["StatusMessage"] = $"{product.Name} was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductEditorViewModel> PrepareAsync(ProductEditorViewModel model)
    {
        model.Categories = await db.Categories.AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
            .ToListAsync();
        return model;
    }

    private async Task ValidateEditorAsync(ProductEditorViewModel model, bool requireImage)
    {
        if (await db.Products.AnyAsync(product => product.Slug == model.Slug && product.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Slug), "This URL slug is already in use.");
        }

        if (!await db.Categories.AnyAsync(category => category.Id == model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Choose a valid category.");
        }

        if (requireImage && model.NewImages.Count == 0)
        {
            ModelState.AddModelError(nameof(model.NewImages), "Add at least one product image.");
        }
    }

    private static ProductEditorViewModel ToEditor(CatalogProduct product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Slug = product.Slug,
        CategoryId = product.CategoryId,
        PriceDollars = product.PriceCents / 100m,
        ShortDescription = product.ShortDescription,
        Description = product.Description,
        Size = product.Size,
        Material = product.Material,
        Finish = product.Finish,
        Badge = product.Badge,
        CardClass = product.CardClass,
        HighlightsText = product.HighlightsText,
        IsFeatured = product.IsFeatured,
        IsPublished = product.IsPublished,
        DisplayOrder = product.DisplayOrder,
        ExistingImages = product.Images.OrderBy(image => image.SortOrder)
            .Select(image => new ExistingImageViewModel(image.Id, image.ImagePath, image.AltText)).ToList()
    };

    private static long? ToCents(decimal? dollars) => dollars is null
        ? null
        : checked((long)Math.Round(dollars.Value * 100m, MidpointRounding.AwayFromZero));

    private static string NormalizeLines(string value) => string.Join(Environment.NewLine,
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string NormalizeSlug(string? slug, string name)
    {
        var source = string.IsNullOrWhiteSpace(slug) ? name : slug;
        return SlugSeparators().Replace(source.Trim().ToLowerInvariant(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugSeparators();
}
