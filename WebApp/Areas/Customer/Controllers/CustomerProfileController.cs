using System.Security.Claims;
using App.BLL.Management;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.SharedLayout;
using WebApp.ViewModels.Customer.CustomerDashboard;
using WebApp.ViewModels.Customer.CustomerProfile;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/profile")]
public class CustomerProfileController : Controller
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerProfileService _managementCustomerProfileService;
    private readonly IWorkspaceLayoutContextProvider _workspaceLayoutContextProvider;

    public CustomerProfileController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerProfileService managementCustomerProfileService,
        IWorkspaceLayoutContextProvider workspaceLayoutContextProvider)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerProfileService = managementCustomerProfileService;
        _workspaceLayoutContextProvider = workspaceLayoutContextProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementCustomerProfileService.GetProfileAsync(access.context!, cancellationToken);
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
        CustomerProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementCustomerProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _managementCustomerProfileService.UpdateProfileAsync(
            access.context!,
            new CustomerProfileUpdateRequest
            {
                Name = edit.Name,
                RegistryCode = edit.RegistryCode,
                BillingEmail = edit.BillingEmail,
                BillingAddress = edit.BillingAddress,
                Phone = edit.Phone,
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
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string customerSlug,
        CustomerProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _managementCustomerProfileService.GetProfileAsync(access.context!, cancellationToken);
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

        var result = await _managementCustomerProfileService.DeleteProfileAsync(access.context!, cancellationToken);
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
        return RedirectToAction("Index", "Dashboard", new { area = "Management", companySlug });
    }

    private async Task<CustomerProfilePageViewModel> BuildViewModelAsync(
        ManagementCustomerDashboardContext context,
        CustomerProfileModel profile,
        CustomerProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        var pageShell = await BuildPageShellAsync(context, cancellationToken);

        return new CustomerProfilePageViewModel
        {
            PageShell = pageShell,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? new CustomerProfileEditViewModel
            {
                Name = profile.Name,
                RegistryCode = profile.RegistryCode,
                BillingEmail = profile.BillingEmail,
                BillingAddress = profile.BillingAddress,
                Phone = profile.Phone,
                IsActive = profile.IsActive
            }
        };
    }

    private async Task<CustomerPageShellViewModel> BuildPageShellAsync(
        ManagementCustomerDashboardContext context,
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

        return new CustomerPageShellViewModel
        {
            Title = UiText.Profile,
            CurrentSectionLabel = UiText.Profile,
            LayoutContext = layoutContext,
            Customer = new CustomerLayoutViewModel
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                CurrentSection = "Profile"
            }
        };
    }

    private async Task<(IActionResult? response, ManagementCustomerDashboardContext? context)> ResolveAccessAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (access.CompanyNotFound || access.CustomerNotFound)
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
