using System.Security.Claims;
using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using FluentResults;
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
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public DashboardController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder)
    {
        _bll = bll;
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

        var auth = await _bll.CompanyMembershipAdmin.AuthorizeManagementAreaAccessAsync(appUserId.Value, companySlug, cancellationToken);
        if (auth.IsFailed)
        {
            return ToAuthorizationActionResult(auth.Errors);
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
                    ManagementCompanySlug = auth.Value.CompanySlug,
                    ManagementCompanyName = auth.Value.CompanyName,
                    CurrentLevel = WorkspaceLevel.ManagementCompany
                },
                cancellationToken),
            CompanySlug = auth.Value.CompanySlug,
            CompanyName = auth.Value.CompanyName
        };

        ViewData["Title"] = title;
        return View(vm);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private IActionResult ToAuthorizationActionResult(IReadOnlyList<IError> errors)
    {
        if (errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        return Forbid();
    }
}
