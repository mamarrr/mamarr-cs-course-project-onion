using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
public class UpdatesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
