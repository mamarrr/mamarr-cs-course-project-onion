using App.BLL.ManagementUsers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Management.Dashboard;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}")]
public class DashboardController : Controller
{
    private readonly IManagementUserAdminService _managementUserAdminService;

    public DashboardController(IManagementUserAdminService managementUserAdminService)
    {
        _managementUserAdminService = managementUserAdminService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        if (auth.CompanyNotFound)
        {
            return NotFound();
        }

        if (auth.IsForbidden)
        {
            return Forbid();
        }

        var vm = new ManagementDashboardPageViewModel
        {
            CompanySlug = auth.Context!.CompanySlug,
            CompanyName = auth.Context.CompanyName
        };

        ViewData["Title"] = "Dashboard";
        return View(vm);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }
}
