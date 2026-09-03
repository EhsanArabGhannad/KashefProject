using System.Diagnostics;
using KashefProject.Models;
using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICatalogService _catalog;

        public HomeController(ILogger<HomeController> logger, ICatalogService catalog)
        {
            _logger = logger;
            _catalog = catalog;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _catalog.GetFeaturedProductsAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
