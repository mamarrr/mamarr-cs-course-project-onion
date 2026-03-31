using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
public class TicketsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id = 1001)
    {
        ViewData["TicketId"] = id;
        return View();
    }

    public IActionResult Activity(int id = 1001)
    {
        ViewData["TicketId"] = id;
        return View();
    }

    public IActionResult Scheduling(int id = 1001)
    {
        ViewData["TicketId"] = id;
        return View();
    }
}
