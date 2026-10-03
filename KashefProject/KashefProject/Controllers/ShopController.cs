using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("shop")]
public class ShopController(ICatalogService catalog) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await catalog.GetProductsAsync());

    [HttpGet("{slug}")]
    public async Task<IActionResult> Product(string slug)
    {
        var product = await catalog.FindProductAsync(slug);
        if (product is null)
        {
            return NotFound();
        }

        var relatedProducts = (await catalog.GetProductsAsync())
            .Where(candidate => candidate.Id != product.Id)
            .OrderByDescending(candidate => candidate.CategorySlug == product.CategorySlug)
            .Take(3)
            .ToArray();

        return View(new ProductDetailViewModel(product, relatedProducts));
    }
}
