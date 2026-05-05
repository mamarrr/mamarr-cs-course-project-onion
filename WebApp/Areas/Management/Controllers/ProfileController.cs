using System.Security.Claims;
using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.Resources.Views;
using FluentResults;
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
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public ProfileController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder)
    {
        _bll = bll;
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

        var profile = await _bll.ManagementCompanyProfiles.GetProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (profile.IsFailed)
        {
            return ToProfileLookupActionResult(profile.Errors);
        }

        return View(await BuildViewModelAsync(profile.Value, null, cancellationToken));
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

        var currentProfile = await _bll.ManagementCompanyProfiles.GetProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (currentProfile.IsFailed)
        {
            return ToProfileLookupActionResult(currentProfile.Errors);
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
        }

        var result = await _bll.ManagementCompanyProfiles.UpdateProfileAsync(
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
            },
            cancellationToken);

        if (result.IsFailed && result.Errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        if (result.IsFailed && result.Errors.OfType<ForbiddenError>().Any())
        {
            return Forbid();
        }

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(result.Errors, UiText.UnableToUpdateProfile));
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
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

        var currentProfile = await _bll.ManagementCompanyProfiles.GetProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (currentProfile.IsFailed)
        {
            return ToProfileLookupActionResult(currentProfile.Errors);
        }

        if (!IsDeleteConfirmationValid(edit.DeleteConfirmation, currentProfile.Value.Name))
        {
            ModelState.AddModelError(nameof(edit.DeleteConfirmation), UiText.DeleteConfirmationDoesNotMatch);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
        }

        var result = await _bll.ManagementCompanyProfiles.DeleteProfileAsync(appUserId.Value, companySlug, cancellationToken);
        if (result.IsFailed && result.Errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        if (result.IsFailed && result.Errors.OfType<ForbiddenError>().Any())
        {
            var errorMessage = result.Errors.FirstOrDefault()?.Message;
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
            }

            return Forbid();
        }

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(result.Errors, UiText.UnableToDeleteProfile));
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
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

    private IActionResult ToProfileLookupActionResult(IReadOnlyList<IError> errors)
    {
        if (errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        return Forbid();
    }

    private static string ErrorMessage(IReadOnlyList<IError> errors, string fallback)
    {
        return errors.FirstOrDefault()?.Message ?? fallback;
    }
}
