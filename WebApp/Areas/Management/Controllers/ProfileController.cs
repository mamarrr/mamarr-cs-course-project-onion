using System.Security.Claims;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.BLL.Contracts.ManagementCompanies.Services;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Profile;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/profile")]
public class ProfileController : Controller
{
    private readonly IManagementCompanyProfileService _managementCompanyProfileService;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public ProfileController(
        IManagementCompanyProfileService managementCompanyProfileService,
        IAppChromeBuilder appChromeBuilder)
    {
        _managementCompanyProfileService = managementCompanyProfileService;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var profile = await _managementCompanyProfileService.GetProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        return View(await BuildViewModelAsync(profile, null, cancellationToken));
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        ManagementCompanyProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var currentProfile = await _managementCompanyProfileService.GetProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (currentProfile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile, edit, cancellationToken));
        }

        var result = await _managementCompanyProfileService.UpdateProfileAsync(
            appUserId.Value,
            companySlug,
            new CompanyProfileUpdateRequest
            {
                Name = edit.Name,
                RegistryCode = edit.RegistryCode,
                VatNumber = edit.VatNumber,
                Email = edit.Email,
                Phone = edit.Phone,
                Address = edit.Address,
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
            return View("Index", await BuildViewModelAsync(currentProfile, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileUpdatedSuccessfully)] = UiText.ProfileUpdatedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        ManagementCompanyProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var currentProfile = await _managementCompanyProfileService.GetProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (currentProfile == null)
        {
            return NotFound();
        }

        if (!IsDeleteConfirmationValid(edit.DeleteConfirmation, currentProfile.Name))
        {
            ModelState.AddModelError(nameof(edit.DeleteConfirmation), UiText.DeleteConfirmationDoesNotMatch);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile, edit, cancellationToken));
        }

        var result = await _managementCompanyProfileService.DeleteProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.Forbidden)
        {
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View("Index", await BuildViewModelAsync(currentProfile, edit, cancellationToken));
            }

            return Forbid();
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? UiText.UnableToDeleteProfile);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
        return RedirectToAction("Index", "Onboarding", new { area = "", showChooser = true });
    }

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        CompanyProfileModel profile,
        ManagementCompanyProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        var title = UiText.Profile;

        return new ProfilePageViewModel
        {
            AppChrome = await _appChromeBuilder.BuildAsync(
                new AppChromeRequest
                {
                    User = User,
                    HttpContext = HttpContext,
                    PageTitle = title,
                    ActiveSection = Sections.Profile,
                    ManagementCompanySlug = profile.CompanySlug,
                    ManagementCompanyName = profile.Name,
                    CurrentLevel = WorkspaceLevel.ManagementCompany
                },
                cancellationToken),
            CompanySlug = profile.CompanySlug,
            CompanyName = profile.Name,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? new ManagementCompanyProfileEditViewModel
            {
                Name = profile.Name,
                RegistryCode = profile.RegistryCode,
                VatNumber = profile.VatNumber,
                Email = profile.Email,
                Phone = profile.Phone,
                Address = profile.Address,
                IsActive = profile.IsActive
            }
        };
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
