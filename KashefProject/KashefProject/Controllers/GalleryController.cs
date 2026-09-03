using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("gallery")]
public class GalleryController(ICatalogService catalog) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await catalog.GetCategoriesAsync());

    [HttpGet("{slug}")]
    public async Task<IActionResult> Category(string slug)
    {
        var category = await catalog.FindCategoryAsync(slug);
        return category is null ? NotFound() : View(category);
    }
}
