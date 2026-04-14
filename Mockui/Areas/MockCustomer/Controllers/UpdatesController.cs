using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockCustomer.Controllers;

[Area("Customer")]
public class UpdatesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
