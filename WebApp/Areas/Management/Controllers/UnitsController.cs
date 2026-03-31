using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
public class UnitsController : Controller
{
    public IActionResult Details(int id = 501)
    {
        ViewData["UnitId"] = id;
        return View();
    }
}
