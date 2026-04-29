using System.Security.Claims;
using App.BLL.Contracts.ManagementCompanies.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Dashboard;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}")]
public class DashboardController : Controller
{
    private readonly ICompanyMembershipAdminService _companyMembershipAdminService;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public DashboardController(
        ICompanyMembershipAdminService companyMembershipAdminService,
        IAppChromeBuilder appChromeBuilder)
    {
        _companyMembershipAdminService = companyMembershipAdminService;
        _appChromeBuilder = appChromeBuilder;
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
        var vm = new DashboardPageViewModel
        {
            AppChrome = await _appChromeBuilder.BuildAsync(
                new AppChromeRequest
                {
                    User = User,
                    HttpContext = HttpContext,
                    PageTitle = title,
                    ActiveSection = Sections.Dashboard,
                    ManagementCompanySlug = auth.Context!.CompanySlug,
                    ManagementCompanyName = auth.Context.CompanyName,
                    CurrentLevel = WorkspaceLevel.ManagementCompany
                },
                cancellationToken),
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
