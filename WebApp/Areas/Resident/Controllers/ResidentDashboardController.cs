using System.Security.Claims;
using App.BLL.ResidentWorkspace.Access;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Resident;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}")]
public class ResidentDashboardController : Controller
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public ResidentDashboardController(
        IResidentAccessService residentAccessService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _residentAccessService = residentAccessService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
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

        var residentLayout = new ResidentLayoutViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = context.ResidentIdCode,
            ResidentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
                ? context.ResidentIdCode
                : context.FullName,
            ResidentSupportingText = string.IsNullOrWhiteSpace(context.FullName)
                ? null
                : context.ResidentIdCode,
            CurrentSection = currentSection
        };

        var pageShell = await BuildPageShellAsync(
            title,
            currentSectionLabel,
            residentLayout.CompanySlug,
            cancellationToken,
            residentLayout);

        var vm = new ResidentDashboardPageViewModel
        {
            PageShell = pageShell,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = context.ResidentIdCode,
            ResidentDisplayName = residentLayout.ResidentDisplayName,
            ResidentSupportingText = residentLayout.ResidentSupportingText,
            CurrentSection = currentSection
        };

        return View("Index", vm);
    }

    private async Task<ResidentPageShellViewModel> BuildPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        ResidentLayoutViewModel residentLayout)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            BuildWorkspaceRequest(companySlug),
            cancellationToken);

        return new ResidentPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            LayoutContext = layoutContext,
            Resident = residentLayout
        };
    }

    private WorkspaceLayoutRequestViewModel BuildWorkspaceRequest(string companySlug)
    {
        return new WorkspaceLayoutRequestViewModel
        {
            CurrentController = ControllerContext.ActionDescriptor.ControllerName,
            CompanySlug = companySlug,
            CurrentPathAndQuery = $"{Request.Path}{Request.QueryString}",
            CurrentUiCultureName = Thread.CurrentThread.CurrentUICulture.Name
        };
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
