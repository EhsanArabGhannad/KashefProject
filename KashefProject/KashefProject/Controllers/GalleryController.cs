using KashefProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("gallery")]
public class GalleryController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(StoreCatalog.Categories);

    [HttpGet("{slug}")]
    public IActionResult Category(string slug)
    {
        var category = StoreCatalog.FindCategory(slug);
        if (category is null)
        {
            return NotFound();
        }

        var products = StoreCatalog.Products.Where(product => product.CategorySlug == category.Slug).ToArray();
        return View(new CategoryPageViewModel(category, products));
    }
}
