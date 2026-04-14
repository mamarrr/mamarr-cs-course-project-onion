using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockManagement.Controllers;

[Area("Management")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
