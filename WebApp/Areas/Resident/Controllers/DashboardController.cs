using System.Security.Claims;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Residents;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IResidentAccessService _residentAccessService;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public DashboardController(
        IResidentAccessService residentAccessService,
        IAppChromeBuilder appChromeBuilder)
    {
        _residentAccessService = residentAccessService;
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
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var access = await _residentAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            residentIdCode,
            cancellationToken);

        if (access.CompanyNotFound || access.ResidentNotFound)
        {
            return NotFound();
        }

        if (access.IsForbidden || access.Context == null)
        {
            return Forbid();
        }

        var context = access.Context;
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
        ResidentDashboardContext context,
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

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
