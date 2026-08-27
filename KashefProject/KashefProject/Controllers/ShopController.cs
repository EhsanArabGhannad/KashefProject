using KashefProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("shop")]
public class ShopController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(StoreCatalog.Products);

    [HttpGet("{slug}")]
    public IActionResult Product(string slug)
    {
        var product = StoreCatalog.FindProduct(slug);
        return product is null ? NotFound() : View(product);
    }
}
