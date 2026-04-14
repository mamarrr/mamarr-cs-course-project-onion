using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockManagement.Controllers;

[Area("Management")]
public class PropertiesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id = 201)
    {
        ViewData["PropertyId"] = id;
        return View();
    }

    public IActionResult Units(int id = 201)
    {
        ViewData["PropertyId"] = id;
        return View();
    }
}
