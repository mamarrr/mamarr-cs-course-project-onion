using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
public class VendorsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id = 401)
    {
        ViewData["VendorId"] = id;
        return View();
    }
}
