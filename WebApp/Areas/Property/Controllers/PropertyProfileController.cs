using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Profiles;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.Shared.Profiles;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Management.CustomerProperties;
using WebApp.ViewModels.Property.PropertyProfile;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Property.Controllers;

[Area("Property")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/p/{propertySlug}/profile")]
public class PropertyProfileController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IManagementPropertyProfileService _managementPropertyProfileService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public PropertyProfileController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IManagementPropertyProfileService managementPropertyProfileService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _managementPropertyProfileService = managementPropertyProfileService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementPropertyProfileService.GetProfileAsync(access.context!, cancellationToken);
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
        PropertyProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementPropertyProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _managementPropertyProfileService.UpdateProfileAsync(
            access.context!,
            new PropertyProfileUpdateRequest
            {
                Name = edit.Name,
                AddressLine = edit.AddressLine,
                City = edit.City,
                PostalCode = edit.PostalCode,
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
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string customerSlug,
        string propertySlug,
        PropertyProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementPropertyProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!IsDeleteConfirmationValid(edit.DeleteConfirmation, profile.Name))
        {
            ModelState.AddModelError(nameof(edit.DeleteConfirmation), UiText.DeleteConfirmationDoesNotMatch);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _managementPropertyProfileService.DeleteProfileAsync(access.context!, cancellationToken);
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
        return RedirectToAction("Index", "CustomerDashboard", new { area = "Customer", companySlug, customerSlug });
    }

    private async Task<PropertyProfilePageViewModel> BuildViewModelAsync(
        PropertyDashboardContext context,
        PropertyProfileModel profile,
        PropertyProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        var pageShell = await BuildPageShellAsync(context, cancellationToken);

        return new PropertyProfilePageViewModel
        {
            PageShell = pageShell,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? new PropertyProfileEditViewModel
            {
                Name = profile.Name,
                AddressLine = profile.AddressLine,
                City = profile.City,
                PostalCode = profile.PostalCode,
                Notes = profile.Notes,
                IsActive = profile.IsActive
            }
        };
    }

    private async Task<PropertyPageShellViewModel> BuildPageShellAsync(
        PropertyDashboardContext context,
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

        return new PropertyPageShellViewModel
        {
            Title = UiText.Profile,
            CurrentSectionLabel = UiText.Profile,
            LayoutContext = layoutContext,
            Property = new PropertyLayoutViewModel
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                CurrentSection = "Profile"
            }
        };
    }

    private async Task<(IActionResult? response, PropertyDashboardContext? context)> ResolveAccessAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
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

        return (null, propertyAccess.Context);
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
