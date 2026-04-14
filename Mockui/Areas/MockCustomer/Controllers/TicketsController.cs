using Microsoft.AspNetCore.Mvc;

namespace WebApp.Mockui.Areas.MockCustomer.Controllers;

[Area("Customer")]
public class TicketsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id = 504)
    {
        ViewData["TicketId"] = id;
        return View();
    }
}
