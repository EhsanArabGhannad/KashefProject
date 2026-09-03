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
        return product is null ? NotFound() : View(product);
    }
}
