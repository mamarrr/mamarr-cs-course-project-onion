using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockCustomer.Controllers;

[Area("Customer")]
public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
