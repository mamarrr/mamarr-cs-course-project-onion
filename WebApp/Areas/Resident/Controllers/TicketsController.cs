using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
public class TicketsController : Controller
{
    public IActionResult MyTickets()
    {
        return View();
    }

    public IActionResult NewTicket()
    {
        return View();
    }

    public IActionResult Details(int id = 701)
    {
        ViewData["TicketId"] = id;
        return View();
    }
}
