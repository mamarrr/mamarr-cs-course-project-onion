using System.Security.Claims;
using App.BLL.ManagementCompany.Profiles;
using App.BLL.Shared.Profiles;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.ManagementLayout;
using WebApp.ViewModels.Management.Profile;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/profile")]
public class ManagementProfileController : ManagementPageShellController
{
    private readonly IManagementCompanyProfileService _managementCompanyProfileService;

    public ManagementProfileController(
        IManagementCompanyProfileService managementCompanyProfileService,
        IManagementLayoutViewModelProvider managementLayoutViewModelProvider)
        : base(managementLayoutViewModelProvider)
    {
        _managementCompanyProfileService = managementCompanyProfileService;
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
            new ManagementCompanyProfileUpdateRequest
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

    private async Task<ManagementCompanyProfilePageViewModel> BuildViewModelAsync(
        ManagementCompanyProfileModel profile,
        ManagementCompanyProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        var title = UiText.Profile;

        return new ManagementCompanyProfilePageViewModel
        {
            PageShell = await BuildManagementPageShellAsync(title, title, profile.CompanySlug, cancellationToken),
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
