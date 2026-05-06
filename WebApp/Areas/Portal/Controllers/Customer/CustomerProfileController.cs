using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers.Errors;
using App.BLL.Contracts.Customers.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Customers;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Customer.CustomerProfile;

namespace WebApp.Areas.Portal.Controllers.Customer;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/profile")]
public class CustomerProfileController : Controller
{
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly CustomerProfileMvcMapper _mapper;
    private readonly IAppBLL _bll;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public CustomerProfileController(
        IAppChromeBuilder appChromeBuilder,
        CustomerProfileMvcMapper mapper,
        IAppBLL bll,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _appChromeBuilder = appChromeBuilder;
        _mapper = mapper;
        _bll = bll;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.CustomerProfile)]
    public async Task<IActionResult> Index(string companySlug, string customerSlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.CustomerProfiles.GetAsync(
            _mapper.ToQuery(companySlug, customerSlug, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        return View(await BuildViewModelAsync(result.Value, null, cancellationToken));
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string customerSlug,
        CustomerProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var currentProfile = await _bll.CustomerProfiles.GetAsync(
            _mapper.ToQuery(companySlug, customerSlug, appUserId.Value),
            cancellationToken);

        if (currentProfile.IsFailed)
        {
            return ToMvcErrorResult(currentProfile.Errors);
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
        }

        var result = await _bll.CustomerProfiles.UpdateAsync(
            _mapper.ToCommand(companySlug, customerSlug, edit, appUserId.Value),
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData[nameof(UiText.ProfileUpdatedSuccessfully)] = UiText.ProfileUpdatedSuccessfully;
            return RedirectToAction(nameof(Index), new { companySlug, customerSlug });
        }

        if (HasNonValidationError(result.Errors))
        {
            return ToMvcErrorResult(result.Errors);
        }

        AddModelErrors(result.Errors);
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string customerSlug,
        CustomerProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var currentProfile = await _bll.CustomerProfiles.GetAsync(
            _mapper.ToQuery(companySlug, customerSlug, appUserId.Value),
            cancellationToken);

        if (currentProfile.IsFailed)
        {
            return ToMvcErrorResult(currentProfile.Errors);
        }

        var result = await _bll.CustomerProfiles.DeleteAsync(
            _mapper.ToDeleteCommand(companySlug, customerSlug, edit, appUserId.Value),
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
            return RedirectToRoute(PortalRouteNames.ManagementDashboard, new { companySlug });
        }

        if (HasNonValidationError(result.Errors))
        {
            return ToMvcErrorResult(result.Errors);
        }

        AddModelErrors(result.Errors);
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("Index", await BuildViewModelAsync(currentProfile.Value, edit, cancellationToken));
    }

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        CustomerProfileModel profile,
        CustomerProfileEditViewModel? edit,
        CancellationToken cancellationToken)
    {
        return new ProfilePageViewModel
        {
            AppChrome = await BuildAppChromeAsync(profile, UiText.Profile, cancellationToken),
            CompanySlug = profile.CompanySlug,
            CompanyName = profile.CompanyName,
            CustomerSlug = profile.Slug,
            CustomerName = profile.Name,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? _mapper.ToEditViewModel(profile)
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        CustomerProfileModel profile,
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
                CustomerSlug = profile.Slug,
                CustomerName = profile.Name,
                CurrentLevel = WorkspaceLevel.Customer
            },
            cancellationToken);
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

    private void AddModelErrors(IReadOnlyList<IError> errors)
    {
        var validationError = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validationError is not null)
        {
            foreach (var failure in validationError.Failures)
            {
                var key = failure.PropertyName == "ConfirmationName"
                    ? nameof(CustomerProfileEditViewModel.DeleteConfirmation)
                    : failure.PropertyName;

                ModelState.AddModelError(key, failure.ErrorMessage);
            }

            return;
        }

        var duplicateError = errors.OfType<DuplicateRegistryCodeError>().FirstOrDefault();
        if (duplicateError is not null)
        {
            ModelState.AddModelError(nameof(CustomerProfileEditViewModel.RegistryCode), duplicateError.Message);
            return;
        }

        ModelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? UiText.UnableToUpdateProfile);
    }

    private static bool HasNonValidationError(IReadOnlyList<IError> errors)
    {
        return errors.Any(error =>
            error is NotFoundError
            || error is ForbiddenError
            || error is UnauthorizedError);
    }

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }
}
