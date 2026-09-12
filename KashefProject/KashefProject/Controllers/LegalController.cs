using Microsoft.AspNetCore.Mvc;

namespace KashefProject.Controllers;

[Route("")]
public sealed class LegalController : Controller
{
    [HttpGet("privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("terms")]
    public IActionResult Terms() => View();

    [HttpGet("shipping-returns")]
    public IActionResult ShippingReturns() => View();
}
