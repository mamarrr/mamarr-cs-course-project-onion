using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Units.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Units;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Unit.Controllers;

[Area("Unit")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}/profile")]
public class ProfileController : Controller
{
    private readonly IUnitProfileService _unitProfileService;
    private readonly UnitMvcMapper _unitMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public ProfileController(
        IUnitProfileService unitProfileService,
        UnitMvcMapper unitMapper,
        IAppChromeBuilder appChromeBuilder)
    {
        _unitProfileService = unitProfileService;
        _unitMapper = unitMapper;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var profile = await _unitProfileService.GetAsync(
            _unitMapper.ToProfileQuery(companySlug, customerSlug, propertySlug, unitSlug, User),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToMvcErrorResult(profile.Errors);
        }

        return View(await BuildViewModelAsync(profile.Value, null, cancellationToken));
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var profile = await _unitProfileService.GetAsync(
            _unitMapper.ToProfileQuery(companySlug, customerSlug, propertySlug, unitSlug, User),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToMvcErrorResult(profile.Errors);
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        var result = await _unitProfileService.UpdateAsync(
            _unitMapper.ToUpdateCommand(companySlug, customerSlug, propertySlug, unitSlug, edit, User),
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyErrors(result.Errors, nameof(UnitProfileEditViewModel.UnitNr), UiText.UnableToUpdateProfile);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileUpdatedSuccessfully)] = UiText.ProfileUpdatedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug = result.Value.UnitSlug });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var profile = await _unitProfileService.GetAsync(
            _unitMapper.ToProfileQuery(companySlug, customerSlug, propertySlug, unitSlug, User),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToMvcErrorResult(profile.Errors);
        }

        var result = await _unitProfileService.DeleteAsync(
            _unitMapper.ToDeleteCommand(companySlug, customerSlug, propertySlug, unitSlug, edit, User),
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyErrors(result.Errors, nameof(edit.DeleteConfirmation), UiText.UnableToDeleteProfile);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
        return RedirectToAction("Units", "Units", new { area = "Property", companySlug, customerSlug, propertySlug });
    }

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        UnitProfileModel profile,
        UnitProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        return new ProfilePageViewModel
        {
            AppChrome = await BuildAppChromeAsync(profile, UiText.Profile, cancellationToken),
            CompanySlug = profile.CompanySlug,
            CompanyName = profile.CompanyName,
            CustomerSlug = profile.CustomerSlug,
            CustomerName = profile.CustomerName,
            PropertySlug = profile.PropertySlug,
            PropertyName = profile.PropertyName,
            UnitSlug = profile.UnitSlug,
            UnitName = profile.UnitNr,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? _unitMapper.ToEditViewModel(profile)
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        UnitProfileModel profile,
        string title,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.Profile,
                ManagementCompanySlug = profile.CompanySlug,
                ManagementCompanyName = profile.CompanyName,
                CustomerSlug = profile.CustomerSlug,
                CustomerName = profile.CustomerName,
                PropertySlug = profile.PropertySlug,
                PropertyName = profile.PropertyName,
                UnitSlug = profile.UnitSlug,
                UnitName = profile.UnitNr,
                CurrentLevel = WorkspaceLevel.Unit
            },
            cancellationToken);
    }

    private void ApplyErrors(IReadOnlyList<IError> errors, string validationFallbackKey, string fallbackMessage)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validation is not null)
        {
            foreach (var failure in validation.Failures)
            {
                var key = failure.PropertyName == nameof(App.BLL.Contracts.Units.Commands.DeleteUnitCommand.ConfirmationUnitNr)
                    ? nameof(UnitProfileEditViewModel.DeleteConfirmation)
                    : validationFallbackKey;

                ModelState.AddModelError(key, failure.ErrorMessage);
            }

            return;
        }

        var error = errors.FirstOrDefault();
        if (error is ForbiddenError)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return;
        }

        ModelState.AddModelError(string.Empty, error?.Message ?? fallbackMessage);
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
}
