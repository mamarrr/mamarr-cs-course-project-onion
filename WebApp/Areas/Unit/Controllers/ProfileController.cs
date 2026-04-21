using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Workspace;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Shared.Layout;
using WebApp.ViewModels.Unit;

namespace WebApp.Areas.Unit.Controllers;

[Area("Unit")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/u/{unitSlug}/profile")]
public class ProfileController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IUnitProfileService _unitProfileService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public ProfileController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IUnitProfileService unitProfileService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _unitProfileService = unitProfileService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
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
        var pageShell = await BuildPageShellAsync(context, cancellationToken);

        return new ProfilePageViewModel
        {
            PageShell = pageShell,
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

    private async Task<UnitPageShellViewModel> BuildPageShellAsync(
        UnitDashboardContext context,
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

        return new UnitPageShellViewModel
        {
            Title = UiText.Profile,
            CurrentSectionLabel = UiText.Profile,
            LayoutContext = layoutContext,
            Unit = new LayoutViewModel
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                UnitSlug = context.UnitSlug,
                UnitName = context.UnitNr,
                CurrentSection = "Profile"
            }
        };
    }

    private async Task<(IActionResult? response, UnitDashboardContext? context)> ResolveAccessAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var customerAccess = await _customerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (customerAccess.CompanyNotFound || customerAccess.CustomerNotFound)
        {
            return (NotFound(), null);
        }

        if (customerAccess.IsForbidden || customerAccess.Context == null)
        {
            return (Forbid(), null);
        }

        var propertyAccess = await _propertyWorkspaceService.ResolvePropertyDashboardContextAsync(
            customerAccess.Context,
            propertySlug,
            cancellationToken);

        if (propertyAccess.PropertyNotFound)
        {
            return (NotFound(), null);
        }

        if (!propertyAccess.IsAuthorized || propertyAccess.Context == null)
        {
            return (Forbid(), null);
        }

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Context,
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
