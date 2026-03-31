using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

public class MockUiController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
