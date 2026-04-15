using System.Security.Claims;
using App.BLL.Management;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Resident;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}")]
public class ResidentDashboardController : Controller
{
    private readonly IManagementResidentAccessService _managementResidentAccessService;

    public ResidentDashboardController(IManagementResidentAccessService managementResidentAccessService)
    {
        _managementResidentAccessService = managementResidentAccessService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Dashboard", cancellationToken);
    }

    [HttpGet("units")]
    public async Task<IActionResult> Units(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        return await RenderSectionAsync(companySlug, residentIdCode, "Units", cancellationToken);
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

        var access = await _managementResidentAccessService.ResolveDashboardAccessAsync(
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

        ViewData["Title"] = currentSection == "Dashboard"
            ? T("ResidentDashboard", "Resident dashboard")
            : currentSectionLabel;
        ViewData["CurrentSectionLabel"] = currentSectionLabel;
        ViewData["ResidentLayout"] = new ResidentLayoutViewModel
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

        var vm = new ResidentDashboardPageViewModel
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

        return View("Index", vm);
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
