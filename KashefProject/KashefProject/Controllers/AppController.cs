using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

public sealed class AppController : Controller
{
    [HttpGet("app")]
    public IActionResult Index() => View();
}
