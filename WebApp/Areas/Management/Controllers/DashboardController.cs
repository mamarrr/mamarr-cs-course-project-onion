using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}")]
public class DashboardController : Controller
{
    [HttpGet("")]
    [HttpGet("dashboard")]
    public IActionResult Index(string companySlug)
    {
        ViewData["CompanySlug"] = companySlug;
        return View();
    }
}
