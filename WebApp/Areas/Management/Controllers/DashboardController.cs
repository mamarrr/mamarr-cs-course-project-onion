using System.Security.Claims;
using App.BLL.ManagementCompany.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.ManagementLayout;
using WebApp.ViewModels.Management.Dashboard;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}")]
public class DashboardController : ManagementPageShellController
{
    private readonly ICompanyMembershipAdminService _companyMembershipAdminService;

    public DashboardController(
        ICompanyMembershipAdminService companyMembershipAdminService,
        IManagementLayoutViewModelProvider managementLayoutViewModelProvider)
        : base(managementLayoutViewModelProvider)
    {
        _companyMembershipAdminService = companyMembershipAdminService;
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

        var auth = await _companyMembershipAdminService.AuthorizeManagementAreaAccessAsync(appUserId.Value, companySlug, cancellationToken);
        if (auth.CompanyNotFound)
        {
            return NotFound();
        }

        if (auth.IsForbidden)
        {
            return Forbid();
        }

        var title = App.Resources.Views.UiText.Dashboard;
        var vm = new ManagementDashboardPageViewModel
        {
            PageShell = await BuildManagementPageShellAsync(title, title, auth.Context!.CompanySlug, cancellationToken),
            CompanySlug = auth.Context.CompanySlug,
            CompanyName = auth.Context.CompanyName
        };

        ViewData["Title"] = title;
        return View(vm);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }
}
