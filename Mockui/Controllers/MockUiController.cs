using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("mockui")]
public class MockUiController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
