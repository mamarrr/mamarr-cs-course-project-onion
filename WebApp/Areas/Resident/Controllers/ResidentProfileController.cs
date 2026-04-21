using System.Security.Claims;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Profiles;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Resident;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Resident.Controllers;

[Area("Resident")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}/profile")]
public class ResidentProfileController : Controller
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly IManagementResidentProfileService _managementResidentProfileService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public ResidentProfileController(
        IResidentAccessService residentAccessService,
        IManagementResidentProfileService managementResidentProfileService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _residentAccessService = residentAccessService;
        _managementResidentProfileService = managementResidentProfileService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string residentIdCode, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementResidentProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        return View(await BuildViewModelAsync(access.context!, profile, null, cancellationToken));
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementResidentProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _managementResidentProfileService.UpdateProfileAsync(
            access.context!,
            new ResidentProfileUpdateRequest
            {
                FirstName = edit.FirstName,
                LastName = edit.LastName,
                IdCode = edit.IdCode,
                PreferredLanguage = edit.PreferredLanguage,
                IsActive = edit.IsActive
            },
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.Forbidden)
        {
            return Forbid();
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToUpdateProfile);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileUpdatedSuccessfully)] = UiText.ProfileUpdatedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode = edit.IdCode.Trim() });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementResidentProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!IsDeleteConfirmationValid(edit.DeleteConfirmation, profile.ResidentIdCode))
        {
            ModelState.AddModelError(nameof(edit.DeleteConfirmation), UiText.DeleteConfirmationDoesNotMatch);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _managementResidentProfileService.DeleteProfileAsync(access.context!, cancellationToken);
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.Forbidden)
        {
            return Forbid();
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToDeleteProfile);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
        return RedirectToAction("Index", "Residents", new { area = "Management", companySlug });
    }

    private async Task<ResidentProfilePageViewModel> BuildViewModelAsync(
        ResidentDashboardContext context,
        ResidentProfileModel profile,
        ResidentProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        var pageShell = await BuildPageShellAsync(context, cancellationToken);
        var residentDisplayName = string.IsNullOrWhiteSpace(context.FullName)
            ? context.ResidentIdCode
            : context.FullName;

        return new ResidentProfilePageViewModel
        {
            PageShell = pageShell,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? new ResidentProfileEditViewModel
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                IdCode = profile.ResidentIdCode,
                PreferredLanguage = profile.PreferredLanguage,
                IsActive = profile.IsActive
            },
            ResidentDisplayName = residentDisplayName,
            ResidentIdCode = profile.ResidentIdCode
        };
    }

    private async Task<ResidentPageShellViewModel> BuildPageShellAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken)
    {
        var layoutContext = await _workspaceLayoutContextProvider.BuildAsync(
            User,
            new WorkspaceLayoutRequestViewModel
            {
                CurrentController = ControllerContext.ActionDescriptor.ControllerName,
                CompanySlug = context.CompanySlug,
                CurrentPathAndQuery = $"{Request.Path}{Request.QueryString}",
                CurrentUiCultureName = Thread.CurrentThread.CurrentUICulture.Name
            },
            cancellationToken);

        return new ResidentPageShellViewModel
        {
            Title = UiText.Profile,
            CurrentSectionLabel = UiText.Profile,
            LayoutContext = layoutContext,
            Resident = new ResidentLayoutViewModel
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                ResidentIdCode = context.ResidentIdCode,
                ResidentDisplayName = string.IsNullOrWhiteSpace(context.FullName) ? context.ResidentIdCode : context.FullName,
                ResidentSupportingText = string.IsNullOrWhiteSpace(context.FullName) ? null : context.ResidentIdCode,
                CurrentSection = "Profile"
            }
        };
    }

    private async Task<(IActionResult? response, ResidentDashboardContext? context)> ResolveAccessAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _residentAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            residentIdCode,
            cancellationToken);

        if (access.CompanyNotFound || access.ResidentNotFound)
        {
            return (NotFound(), null);
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, access.Context);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static bool IsDeleteConfirmationValid(string? providedValue, string expectedValue)
    {
        return string.Equals(providedValue?.Trim(), expectedValue.Trim(), StringComparison.Ordinal);
    }
}
