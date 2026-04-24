using System.Security.Claims;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Profiles;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Profiles;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerProfile;

namespace WebApp.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
[Route("m/{companySlug}/c/{customerSlug}/profile")]
public class CustomerProfileController : Controller
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly ICustomerProfileService _customerProfileService;
    private readonly IAppChromeBuilder _appChromeBuilder;

    public CustomerProfileController(
        ICustomerAccessService customerAccessService,
        ICustomerProfileService customerProfileService,
        IAppChromeBuilder appChromeBuilder)
    {
        _customerAccessService = customerAccessService;
        _customerProfileService = customerProfileService;
        _appChromeBuilder = appChromeBuilder;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.response is not null)
        {
            return access.response;
        }

        var profile = await _customerProfileService.GetProfileAsync(access.context!, cancellationToken);
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

        var profile = await _customerProfileService.GetProfileAsync(access.context!, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(access.context!, profile, edit, cancellationToken));
        }

        var result = await _customerProfileService.UpdateProfileAsync(
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

        var profile = await _customerProfileService.GetProfileAsync(access.context!, cancellationToken);
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

        var result = await _customerProfileService.DeleteProfileAsync(access.context!, cancellationToken);
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

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        CustomerWorkspaceDashboardContext context,
        CustomerProfileModel profile,
        CustomerProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        return new ProfilePageViewModel
        {
            AppChrome = await BuildAppChromeAsync(context, UiText.Profile, cancellationToken),
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

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        CustomerWorkspaceDashboardContext context,
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
                CurrentLevel = WorkspaceLevel.Customer
            },
            cancellationToken);
    }

    private async Task<(IActionResult? response, CustomerWorkspaceDashboardContext? context)> ResolveAccessAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var access = await _customerAccessService.ResolveDashboardAccessAsync(
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
