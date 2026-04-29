using System.Security.Claims;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Properties.Services;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Workspace;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Properties;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Unit.Controllers;

[Area("Unit")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}")]
public class DashboardController : Controller
{
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly PropertyMvcMapper _propertyMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public DashboardController(
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        PropertyMvcMapper propertyMapper,
        IAppChromeBuilder appChromeBuilder)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _propertyMapper = propertyMapper;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, unitSlug, "Dashboard", cancellationToken);
    }

    [HttpGet("details")]
    public async Task<IActionResult> Details(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, unitSlug, "Details", cancellationToken);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, unitSlug, "Tickets", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var context = access.context!;
        ViewData["Title"] = T("UnitDashboard", "Unit dashboard");
        ViewData["CurrentSectionLabel"] = currentSection switch
        {
            "Details" => T("Details", "Details"),
            "Tenants" => T("Tenants", "Tenants"),
            "Tickets" => UiText.Tickets,
            _ => UiText.Dashboard
        };

        var vm = new DashboardPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(
                context,
                ViewData["Title"] as string ?? T("UnitDashboard", "Unit dashboard"),
                currentSection,
                cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            CurrentSection = currentSection
        };

        return View("Index", vm);
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        UnitDashboardContext context,
        string title,
        string activeSection,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = activeSection,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                UnitSlug = context.UnitSlug,
                UnitName = context.UnitNr,
                CurrentLevel = WorkspaceLevel.Unit
            },
            cancellationToken);
    }

    private async Task<(IActionResult? response, UnitDashboardContext? context)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var propertyAccess = await _propertyWorkspaceService.GetWorkspaceAsync(
            _propertyMapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);
        if (propertyAccess.IsFailed)
        {
            return (ToMvcErrorResult(propertyAccess.Errors), null);
        }

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Value,
            unitSlug,
            cancellationToken);

        if (unitAccess.UnitNotFound)
        {
            return (NotFound(), null);
        }

        if (!unitAccess.IsAuthorized || unitAccess.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, unitAccess.Context);
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private IActionResult ToMvcErrorResult(IReadOnlyList<IError> errors)
    {
        var error = errors.FirstOrDefault();
        return error switch
        {
            UnauthorizedError => Challenge(),
            NotFoundError => NotFound(),
            ForbiddenError => Forbid(),
            _ => BadRequest()
        };
    }
}
