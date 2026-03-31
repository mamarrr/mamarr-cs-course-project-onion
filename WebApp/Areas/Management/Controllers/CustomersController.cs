using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
public class CustomersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id = 301)
    {
        ViewData["CustomerId"] = id;
        return View();
    }
}
