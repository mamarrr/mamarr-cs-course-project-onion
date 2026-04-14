using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockCustomer.Controllers;

[Area("Customer")]
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
}
