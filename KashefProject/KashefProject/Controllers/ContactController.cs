using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("contact")]
public class ContactController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
