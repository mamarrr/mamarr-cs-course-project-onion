using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
public class SupportController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
