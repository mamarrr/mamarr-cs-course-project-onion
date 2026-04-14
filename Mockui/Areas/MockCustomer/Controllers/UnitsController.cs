using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockCustomer.Controllers;

[Area("Customer")]
public class UnitsController : Controller
{
    public IActionResult Index(int? propertyId = null)
    {
        ViewData["PropertyId"] = propertyId;
        return View();
    }

    public IActionResult Details(int id = 501)
    {
        ViewData["UnitId"] = id;
        return View();
    }
}
