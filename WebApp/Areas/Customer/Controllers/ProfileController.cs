using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
