using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Dashboards;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Dashboard;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}")]
public class DashboardController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public DashboardController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var portalContext = _portalContextResolver.Resolve();
        if (!portalContext.IsAuthenticated)
        {
            return Challenge();
        }

        var resolvedCompanySlug = portalContext.CompanySlug ?? companySlug;
        var dashboard = await _bll.PortalDashboards.GetManagementDashboardAsync(
            new ManagementCompanyRoute
            {
                AppUserId = portalContext.AppUserId!.Value,
                CompanySlug = resolvedCompanySlug
            },
            cancellationToken);
        if (dashboard.IsFailed)
        {
            return ToAuthorizationActionResult(dashboard.Errors);
        }

        var context = dashboard.Value.Context;
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
                    ManagementCompanySlug = context.CompanySlug,
                    ManagementCompanyName = context.CompanyName,
                    CurrentLevel = WorkspaceLevel.ManagementCompany
                },
                cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            Dashboard = PortalDashboardViewModelMapper.Map(dashboard.Value)
        };
        return View(vm);
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
