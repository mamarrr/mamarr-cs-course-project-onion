using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Residents.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Residents;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Resident;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}")]
public class DashboardController : Controller
{
    private readonly IAppBLL _bll;
    private readonly ResidentMvcMapper _residentMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public DashboardController(
        IAppBLL bll,
        ResidentMvcMapper residentMapper,
        IAppChromeBuilder appChromeBuilder)
    {
        _bll = bll;
        _residentMapper = residentMapper;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Dashboard", cancellationToken);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Tickets", cancellationToken);
    }

    [HttpGet("representations")]
    public async Task<IActionResult> Representations(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Representations", cancellationToken);
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Contacts", cancellationToken);
    }

    private async Task<IActionResult> RenderSectionAsync(
        string companySlug,
        string residentIdCode,
        string currentSection,
        CancellationToken cancellationToken)
    {
        var workspace = await _bll.ResidentAccess.ResolveResidentWorkspaceAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);
        if (workspace.IsFailed)
        {
            return ToFailureResult(workspace.Errors);
        }

        var context = workspace.Value;
        var currentSectionLabel = currentSection switch
        {
            "Units" => T("Units", "Units"),
            "Tickets" => UiText.Tickets,
            "Representations" => T("Representations", "Representations"),
            "Contacts" => T("Contacts", "Contacts"),
            _ => UiText.Dashboard
        };

        var title = currentSection == "Dashboard"
            ? T("ResidentDashboard", "Resident dashboard")
            : currentSectionLabel;

        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
            ? context.ResidentIdCode
            : context.FullName;
        var residentSupportingText = string.IsNullOrWhiteSpace(context.FullName)
            ? null
            : context.ResidentIdCode;

        var vm = new DashboardPageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, title, currentSection, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = context.ResidentIdCode,
            ResidentDisplayName = residentDisplayName,
            ResidentSupportingText = residentSupportingText,
            CurrentSection = currentSection
        };

        return View("Index", vm);
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        ResidentWorkspaceModel context,
        string title,
        string activeSection,
        CancellationToken cancellationToken)
    {
        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
            ? context.ResidentIdCode
            : context.FullName;

        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = activeSection,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                ResidentIdCode = context.ResidentIdCode,
                ResidentDisplayName = residentDisplayName,
                ResidentSupportingText = string.IsNullOrWhiteSpace(context.FullName) ? null : context.ResidentIdCode,
                CurrentLevel = WorkspaceLevel.Resident
            },
            cancellationToken);
    }

    private IActionResult ToFailureResult(IReadOnlyList<IError> errors)
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
