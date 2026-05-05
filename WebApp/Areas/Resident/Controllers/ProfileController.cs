using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Residents.Errors;
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
[Route("m/{companySlug}/r/{residentIdCode}/profile")]
public class ProfileController : Controller
{
    private readonly IAppBLL _bll;
    private readonly ResidentMvcMapper _residentMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public ProfileController(
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
        var result = await _bll.ResidentProfiles.GetAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);

        if (result.IsFailed)
        {
            return ToFailureResult(result.Errors);
        }

        return View(await BuildViewModelAsync(result.Value, null, cancellationToken));
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var profile = await _bll.ResidentProfiles.GetAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToFailureResult(profile.Errors);
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        var result = await _bll.ResidentProfiles.UpdateAsync(
            _residentMapper.ToUpdateCommand(companySlug, residentIdCode, edit, User),
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessFailure(result.Errors))
            {
                return ToFailureResult(result.Errors);
            }

            ApplyProfileErrors(result.Errors, edit);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileUpdatedSuccessfully)] = UiText.ProfileUpdatedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode = result.Value.ResidentIdCode });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string residentIdCode,
        ResidentProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var profile = await _bll.ResidentProfiles.GetAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToFailureResult(profile.Errors);
        }

        var result = await _bll.ResidentProfiles.DeleteAsync(
            _residentMapper.ToDeleteCommand(companySlug, residentIdCode, edit, User),
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessFailure(result.Errors))
            {
                return ToFailureResult(result.Errors);
            }

            ApplyDeleteErrors(result.Errors, edit);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
        return RedirectToAction("Index", "Residents", new { area = "Management", companySlug });
    }

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        ResidentProfileModel profile,
        ResidentProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        var residentDisplayName = string.IsNullOrWhiteSpace(profile.FullName)
            ? profile.ResidentIdCode
            : profile.FullName;

        return new ProfilePageViewModel
        {
            AppChrome = await BuildAppChromeAsync(profile, UiText.Profile, cancellationToken),
            CompanySlug = profile.CompanySlug,
            CompanyName = profile.CompanyName,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? _residentMapper.ToEditViewModel(profile),
            ResidentDisplayName = residentDisplayName,
            ResidentIdCode = profile.ResidentIdCode
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        ResidentProfileModel profile,
        string title,
        CancellationToken cancellationToken)
    {
        var residentDisplayName = string.IsNullOrWhiteSpace(profile.FullName)
            ? profile.ResidentIdCode
            : profile.FullName;

        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.Profile,
                ManagementCompanySlug = profile.CompanySlug,
                ManagementCompanyName = profile.CompanyName,
                ResidentIdCode = profile.ResidentIdCode,
                ResidentDisplayName = residentDisplayName,
                ResidentSupportingText = string.IsNullOrWhiteSpace(profile.FullName) ? null : profile.ResidentIdCode,
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

    private static bool HasAccessFailure(IReadOnlyList<IError> errors)
    {
        return errors.Any(error =>
            error is UnauthorizedError
                or NotFoundError
                or ForbiddenError);
    }

    private void ApplyProfileErrors(IReadOnlyList<IError> errors, ResidentProfileEditViewModel edit)
    {
        var duplicate = errors.OfType<DuplicateResidentIdCodeError>().FirstOrDefault();
        if (duplicate is not null)
        {
            ModelState.AddModelError($"{nameof(ProfilePageViewModel.Edit)}.{nameof(edit.IdCode)}", duplicate.Message);
            return;
        }

        ApplyValidationErrors(errors, PrefixEditProperty);
        if (ModelState.ErrorCount == 0)
        {
            ModelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? UiText.UnableToUpdateProfile);
        }
    }

    private void ApplyDeleteErrors(IReadOnlyList<IError> errors, ResidentProfileEditViewModel edit)
    {
        ApplyValidationErrors(errors, PrefixEditProperty);
        if (ModelState.ErrorCount == 0)
        {
            ModelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? UiText.UnableToDeleteProfile);
        }
    }

    private void ApplyValidationErrors(
        IReadOnlyList<IError> errors,
        Func<string?, string> mapPropertyName)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validation is not null)
        {
            foreach (var failure in validation.Failures)
            {
                ModelState.AddModelError(mapPropertyName(failure.PropertyName), failure.ErrorMessage);
            }
        }

        var residentValidation = errors.OfType<ResidentValidationError>().FirstOrDefault();
        if (residentValidation is null)
        {
            return;
        }

        foreach (var failure in residentValidation.Failures)
        {
            ModelState.AddModelError(mapPropertyName(failure.PropertyName), failure.ErrorMessage);
        }
    }

    private static string PrefixEditProperty(string? propertyName)
    {
        return propertyName switch
        {
            "ConfirmationIdCode" =>
                $"{nameof(ProfilePageViewModel.Edit)}.{nameof(ResidentProfileEditViewModel.DeleteConfirmation)}",
            "FirstName" =>
                $"{nameof(ProfilePageViewModel.Edit)}.{nameof(ResidentProfileEditViewModel.FirstName)}",
            "LastName" =>
                $"{nameof(ProfilePageViewModel.Edit)}.{nameof(ResidentProfileEditViewModel.LastName)}",
            "IdCode" =>
                $"{nameof(ProfilePageViewModel.Edit)}.{nameof(ResidentProfileEditViewModel.IdCode)}",
            _ => string.Empty
        };
    }
}
