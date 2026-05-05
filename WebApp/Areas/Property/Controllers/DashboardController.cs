using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Properties.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Properties;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Property;

namespace WebApp.Areas.Property.Controllers;

[Area("Property")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}")]
public class DashboardController : Controller
{
    private readonly IAppBLL _bll;
    private readonly PropertyMvcMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public DashboardController(
        IAppBLL bll,
        PropertyMvcMapper mapper,
        IAppChromeBuilder appChromeBuilder)
    {
        _bll = bll;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Dashboard", cancellationToken);
    }

    [HttpGet("residents")]
    public async Task<IActionResult> Residents(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Residents", cancellationToken);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, customerSlug, propertySlug, "Tickets", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var result = await _bll.PropertyWorkspaces.GetWorkspaceAsync(
            _mapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var currentSectionLabel = currentSection switch
        {
            "Profile" => UiText.Profile,
            "Residents" => UiText.Residents,
            "Tickets" => UiText.Tickets,
            _ => UiText.Dashboard
        };

        var title = currentSection == Sections.Dashboard
            ? T("PropertyDashboard", "Property dashboard")
            : currentSectionLabel;

        var vm = new DashboardPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(result.Value, title, currentSection, cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            CustomerSlug = result.Value.CustomerSlug,
            CustomerName = result.Value.CustomerName,
            PropertySlug = result.Value.PropertySlug,
            PropertyName = result.Value.PropertyName,
            CurrentSection = currentSection
        };

        return View("Index", vm);
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        PropertyWorkspaceModel context,
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
                CurrentLevel = WorkspaceLevel.Property
            },
            cancellationToken);
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

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
