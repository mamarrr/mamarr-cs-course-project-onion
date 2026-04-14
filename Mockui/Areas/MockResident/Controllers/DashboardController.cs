using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockResident.Controllers;

[Area("Resident")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
