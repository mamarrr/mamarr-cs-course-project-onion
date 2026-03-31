using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
