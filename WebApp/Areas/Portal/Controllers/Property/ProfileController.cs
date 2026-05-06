using App.BLL.Contracts;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Properties.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.Mappers.Mvc.Properties;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Property;

namespace WebApp.Areas.Portal.Controllers.Property;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile")]
public class ProfileController : Controller
{
    private readonly IAppBLL _bll;
    private readonly PropertyMvcMapper _mapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public ProfileController(
        IAppBLL bll,
        PropertyMvcMapper mapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _mapper = mapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.PropertyProfile)]
    public async Task<IActionResult> Index(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var profile = await _bll.PropertyProfiles.GetAsync(
            _mapper.ToProfileQuery(companySlug, customerSlug, propertySlug, appUserId.Value),
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
        PropertyProfileEditViewModel edit,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var profile = await _bll.PropertyProfiles.GetAsync(
            _mapper.ToProfileQuery(companySlug, customerSlug, propertySlug, appUserId.Value),
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

        var result = await _bll.PropertyProfiles.UpdateAsync(
            _mapper.ToUpdateCommand(companySlug, customerSlug, propertySlug, edit, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            if (IsAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            ApplyValidationErrors(result.Errors, ModelState);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileUpdatedSuccessfully)] = UiText.ProfileUpdatedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug, customerSlug, propertySlug = result.Value.PropertySlug });
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
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var profile = await _bll.PropertyProfiles.GetAsync(
            _mapper.ToProfileQuery(companySlug, customerSlug, propertySlug, appUserId.Value),
            cancellationToken);
        if (profile.IsFailed)
        {
            return ToMvcErrorResult(profile.Errors);
        }

        var result = await _bll.PropertyProfiles.DeleteAsync(
            _mapper.ToDeleteCommand(companySlug, customerSlug, propertySlug, edit, appUserId.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            if (IsAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            ApplyValidationErrors(result.Errors, ModelState);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", await BuildViewModelAsync(profile.Value, edit, cancellationToken));
        }

        TempData[nameof(UiText.ProfileDeletedSuccessfully)] = UiText.ProfileDeletedSuccessfully;
        return RedirectToRoute(PortalRouteNames.CustomerDashboard, new { companySlug, customerSlug });
    }

    private async Task<ProfilePageViewModel> BuildViewModelAsync(
        PropertyProfileModel profile,
        PropertyProfileEditViewModel? edit,
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
            PropertyName = profile.Name,
            SuccessMessage = TempData[nameof(UiText.ProfileUpdatedSuccessfully)] as string,
            Edit = edit ?? _mapper.ToEditViewModel(profile)
        };
    }

    private Task<AppChromeViewModel> BuildAppChromeAsync(
        PropertyProfileModel profile,
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
                PropertyName = profile.Name,
                CurrentLevel = WorkspaceLevel.Property
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

    private static bool IsAccessError(IReadOnlyList<IError> errors)
    {
        return errors.Any(error => error is UnauthorizedError or NotFoundError or ForbiddenError);
    }

    private static void ApplyValidationErrors(IReadOnlyList<IError> errors, ModelStateDictionary modelState)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validation is not null)
        {
            foreach (var failure in validation.Failures)
            {
                modelState.AddModelError(failure.PropertyName ?? string.Empty, failure.ErrorMessage);
            }

            return;
        }

        modelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? UiText.UnableToUpdateProfile);
    }

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }
}
