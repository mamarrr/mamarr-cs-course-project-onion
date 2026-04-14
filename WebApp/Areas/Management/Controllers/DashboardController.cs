using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
