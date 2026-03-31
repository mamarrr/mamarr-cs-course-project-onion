using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
public class PropertiesController : Controller
{
    public IActionResult Details(int id = 201)
    {
        ViewData["PropertyId"] = id;
        return View();
    }
}
