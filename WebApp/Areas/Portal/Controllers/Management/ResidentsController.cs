using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Residents.Errors;
using App.BLL.DTO.Residents.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Residents;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Residents;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/residents")]
public class ResidentsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly ResidentMvcMapper _residentMapper;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;
    private readonly ILogger<ResidentsController> _logger;

    public ResidentsController(
        IAppBLL bll,
        ResidentMvcMapper residentMapper,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver,
        ILogger<ResidentsController> logger)
    {
        _bll = bll;
        _residentMapper = residentMapper;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
        _logger = logger;
    }

    [HttpGet("", Name = PortalRouteNames.ManagementResidents)]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        var result = await _bll.ResidentWorkspaces.GetResidentsAsync(
            _residentMapper.ToResidentsQuery(companySlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToFailureResult(result.Errors);
        }

        var pageVm = await BuildPageViewModelAsync(result.Value, cancellationToken);
        return View(pageVm);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        ResidentsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Challenge();
        }

        _logger.LogInformation(
            "Management residents add started for companySlug={CompanySlug}, hasFirstName={HasFirstName}, hasLastName={HasLastName}, hasIdCode={HasIdCode}",
            companySlug,
            !string.IsNullOrWhiteSpace(vm.AddResident.FirstName),
            !string.IsNullOrWhiteSpace(vm.AddResident.LastName),
            !string.IsNullOrWhiteSpace(vm.AddResident.IdCode));

        var residents = await _bll.ResidentWorkspaces.GetResidentsAsync(
            _residentMapper.ToResidentsQuery(companySlug, appUserId.Value),
            cancellationToken);
        if (residents.IsFailed)
        {
            _logger.LogWarning(
                "Management residents add authorization failed for companySlug={CompanySlug}",
                companySlug);
            return ToFailureResult(residents.Errors);
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPageViewModelAsync(residents.Value, cancellationToken, vm.AddResident);
            return View(nameof(Index), invalidVm);
        }

        var createResult = await _bll.ResidentWorkspaces.CreateAsync(
            _residentMapper.ToCreateCommand(companySlug, vm.AddResident, appUserId.Value),
            cancellationToken);

        if (createResult.IsFailed)
        {
            _logger.LogWarning(
                "Management residents add business validation failed for companySlug={CompanySlug}, errors={Errors}",
                companySlug,
                string.Join("; ", createResult.Errors.Select(error => error.Message)));

            ApplyCreateErrors(createResult.Errors);

            var invalidVm = await BuildPageViewModelAsync(residents.Value, cancellationToken, vm.AddResident);
            return View(nameof(Index), invalidVm);
        }

        TempData["ManagementResidentsSuccess"] = T("ResidentAddedSuccessfully", "Resident added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    private async Task<ResidentsPageViewModel> BuildPageViewModelAsync(
        CompanyResidentsModel model,
        CancellationToken cancellationToken,
        AddManagementResidentViewModel? addResidentOverride = null)
    {
        var title = T("Residents", "Residents");

        return new ResidentsPageViewModel
        {
            AppChrome = await _appChromeBuilder.BuildAsync(
                new AppChromeRequest
                {
                    User = User,
                    HttpContext = HttpContext,
                    PageTitle = title,
                    ActiveSection = Sections.Residents,
                    ManagementCompanySlug = model.CompanySlug,
                    ManagementCompanyName = model.CompanyName,
                    CurrentLevel = WorkspaceLevel.ManagementCompany
                },
                cancellationToken),
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Residents = model.Residents
                .Select(x => new ManagementResidentListItemViewModel
                {
                    ResidentId = x.ResidentId,
                    FullName = x.FullName,
                    IdCode = x.IdCode,
                    PreferredLanguage = x.PreferredLanguage,
                })
                .ToList(),
            AddResident = addResidentOverride ?? new AddManagementResidentViewModel()
        };
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

    private void ApplyCreateErrors(IReadOnlyList<IError> errors)
    {
        var duplicate = errors.OfType<DuplicateResidentIdCodeError>().FirstOrDefault();
        if (duplicate is not null)
        {
            ModelState.AddModelError("AddResident.IdCode", duplicate.Message);
            return;
        }

        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validation is not null)
        {
            foreach (var failure in validation.Failures)
            {
                ModelState.AddModelError($"AddResident.{failure.PropertyName}", failure.ErrorMessage);
            }

            return;
        }

        var residentValidation = errors.OfType<ResidentValidationError>().FirstOrDefault();
        if (residentValidation is not null)
        {
            foreach (var failure in residentValidation.Failures)
            {
                ModelState.AddModelError($"AddResident.{failure.PropertyName}", failure.ErrorMessage);
            }

            return;
        }

        ModelState.AddModelError(string.Empty, errors.FirstOrDefault()?.Message ?? T("UnableToAddResident", "Unable to add resident."));
    }

    private Guid? GetAppUserId()
    {
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
