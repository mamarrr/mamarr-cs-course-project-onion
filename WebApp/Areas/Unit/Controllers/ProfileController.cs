using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Properties.Services;
using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Workspace;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Properties;
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
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IUnitProfileService _unitProfileService;
    private readonly PropertyMvcMapper _propertyMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public ProfileController(
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IUnitProfileService unitProfileService,
        PropertyMvcMapper propertyMapper,
        IAppChromeBuilder appChromeBuilder)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _unitProfileService = unitProfileService;
        _propertyMapper = propertyMapper;
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
        var access = await ResolveAccessAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _unitProfileService.GetProfileAsync(access.context!, cancellationToken);
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
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _unitProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _unitProfileService.UpdateProfileAsync(
            access.context!,
            new UnitProfileUpdateRequest
            {
                UnitNr = edit.UnitNr,
                FloorNr = edit.FloorNr,
                SizeM2 = edit.SizeM2,
                Notes = edit.Notes,
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
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug, unitSlug = access.context!.UnitSlug });
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
        var access = await ResolveAccessAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _unitProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!IsDeleteConfirmationValid(edit.DeleteConfirmation, profile.UnitNr))
        {
            ModelState.AddModelError(nameof(edit.DeleteConfirmation), UiText.DeleteConfirmationDoesNotMatch);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _unitProfileService.DeleteProfileAsync(access.context!, cancellationToken);
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
        return RedirectToAction("Units", "Units", new { area = "Property", companySlug, customerSlug, propertySlug });
    }

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        UnitDashboardContext context,
        UnitProfileModel profile,
        UnitProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        return new ProfilePageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, UiText.Profile, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? new UnitProfileEditViewModel
            {
                UnitNr = profile.UnitNr,
                FloorNr = profile.FloorNr,
                SizeM2 = profile.SizeM2,
                Notes = profile.Notes,
                IsActive = profile.IsActive
            }
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        UnitDashboardContext context,
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
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                UnitSlug = context.UnitSlug,
                UnitName = context.UnitNr,
                CurrentLevel = WorkspaceLevel.Unit
            },
            cancellationToken);
    }

    private async Task<(IActionResult? response, UnitDashboardContext? context)> ResolveAccessAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var propertyAccess = await _propertyWorkspaceService.GetWorkspaceAsync(
            _propertyMapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);
        if (propertyAccess.IsFailed)
        {
            return (ToMvcErrorResult(propertyAccess.Errors), null);
        }

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Value,
            unitSlug,
            cancellationToken);

        if (unitAccess.UnitNotFound)
        {
            return (NotFound(), null);
        }

        if (!unitAccess.IsAuthorized || unitAccess.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, unitAccess.Context);
    }

    private static bool IsDeleteConfirmationValid(string? providedValue, string expectedValue)
    {
        return string.Equals(providedValue?.Trim(), expectedValue.Trim(), StringComparison.Ordinal);
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
