using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
public class UnitsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id = 301)
    {
        ViewData["UnitId"] = id;
        return View();
    }
}
