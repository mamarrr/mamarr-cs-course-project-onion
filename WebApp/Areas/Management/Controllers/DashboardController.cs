using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
