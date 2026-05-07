using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Units;
using App.BLL.DTO.Units.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Portal.Controllers.Unit;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile")]
public class ProfileController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public ProfileController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.UnitProfile)]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var profile = await _bll.Units.GetProfileAsync(
            ToRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
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
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var profile = await _bll.Units.GetProfileAsync(
            ToRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
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

        var result = await _bll.Units.UpdateAndGetProfileAsync(
            ToRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            ToDto(edit),
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
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var profile = await _bll.Units.GetProfileAsync(
            ToRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToMvcErrorResult(profile.Errors);
        }

        var result = await _bll.Units.DeleteAsync(
            ToRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            edit.DeleteConfirmation ?? string.Empty,
            cancellationToken);

        if (result.IsFailed)
        {
            ApplyErrors(result.Errors, nameof(edit.DeleteConfirmation), UiText.UnableToDeleteProfile);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
        return RedirectToRoute(PortalRouteNames.PropertyUnits, new { companySlug, customerSlug, propertySlug });
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
            Edit = edit ?? ToEditViewModel(profile)
        };
    }

    private static UnitProfileEditViewModel ToEditViewModel(UnitProfileModel profile)
    {
        return new UnitProfileEditViewModel
        {
            UnitNr = profile.UnitNr,
            FloorNr = profile.FloorNr,
            SizeM2 = profile.SizeM2,
            Notes = profile.Notes
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
                var key = failure.PropertyName == "ConfirmationUnitNr"
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

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static UnitRoute ToRoute(string companySlug, string customerSlug, string propertySlug, string unitSlug, Guid appUserId)
    {
        return new UnitRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug
        };
    }

    private static UnitBllDto ToDto(UnitProfileEditViewModel edit)
    {
        return new UnitBllDto
        {
            UnitNr = edit.UnitNr,
            FloorNr = edit.FloorNr,
            SizeM2 = edit.SizeM2,
            Notes = edit.Notes
        };
    }
}
