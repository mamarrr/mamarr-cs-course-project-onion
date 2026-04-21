using System.Security.Claims;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Residents;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.ManagementLayout;
using WebApp.ViewModels.Management.Residents;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/residents")]
public class ResidentsController : ManagementPageShellController
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly ICompanyResidentService _companyResidentService;
    private readonly ILogger<ResidentsController> _logger;

    public ResidentsController(
        IResidentAccessService residentAccessService,
        ICompanyResidentService companyResidentService,
        ILogger<ResidentsController> logger,
        IManagementLayoutViewModelProvider managementLayoutViewModelProvider)
        : base(managementLayoutViewModelProvider)
    {
        _residentAccessService = residentAccessService;
        _companyResidentService = companyResidentService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(companySlug, cancellationToken);
        if (authResult.response is not null)
        {
            return authResult.response;
        }

        var pageVm = await BuildPageViewModelAsync(authResult.context!, cancellationToken);
        return View(pageVm);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        ResidentsPageViewModel vm,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Management residents add started for companySlug={CompanySlug}, hasFirstName={HasFirstName}, hasLastName={HasLastName}, hasIdCode={HasIdCode}",
            companySlug,
            !string.IsNullOrWhiteSpace(vm.AddResident.FirstName),
            !string.IsNullOrWhiteSpace(vm.AddResident.LastName),
            !string.IsNullOrWhiteSpace(vm.AddResident.IdCode));

        var authResult = await AuthorizeAsync(companySlug, cancellationToken);
        if (authResult.response is not null)
        {
            _logger.LogWarning(
                "Management residents add authorization failed for companySlug={CompanySlug}, responseType={ResponseType}",
                companySlug,
                authResult.response.GetType().Name);
            return authResult.response;
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPageViewModelAsync(authResult.context!, cancellationToken, vm.AddResident);
            return View(nameof(Index), invalidVm);
        }

        var createResult = await _companyResidentService.CreateAsync(
            authResult.context!,
            new ResidentCreateRequest
            {
                FirstName = vm.AddResident.FirstName,
                LastName = vm.AddResident.LastName,
                IdCode = vm.AddResident.IdCode,
                PreferredLanguage = vm.AddResident.PreferredLanguage
            },
            cancellationToken);

        if (!createResult.Success)
        {
            _logger.LogWarning(
                "Management residents add business validation failed for companySlug={CompanySlug}, duplicateIdCode={DuplicateIdCode}, invalidFirstName={InvalidFirstName}, invalidLastName={InvalidLastName}, invalidIdCode={InvalidIdCode}, errorMessage={ErrorMessage}",
                companySlug,
                createResult.DuplicateIdCode,
                createResult.InvalidFirstName,
                createResult.InvalidLastName,
                createResult.InvalidIdCode,
                createResult.ErrorMessage);

            if (createResult.DuplicateIdCode)
            {
                ModelState.AddModelError("AddResident.IdCode", createResult.ErrorMessage ?? T("ResidentIdCodeAlreadyExists", "Resident with this ID code already exists in this company."));
            }
            else if (createResult.InvalidFirstName)
            {
                ModelState.AddModelError("AddResident.FirstName", createResult.ErrorMessage ?? UiText.RequiredField);
            }
            else if (createResult.InvalidLastName)
            {
                ModelState.AddModelError("AddResident.LastName", createResult.ErrorMessage ?? UiText.RequiredField);
            }
            else if (createResult.InvalidIdCode)
            {
                ModelState.AddModelError("AddResident.IdCode", createResult.ErrorMessage ?? UiText.RequiredField);
            }
            else
            {
                ModelState.AddModelError(string.Empty, createResult.ErrorMessage ?? T("UnableToAddResident", "Unable to add resident."));
            }

            var invalidVm = await BuildPageViewModelAsync(authResult.context!, cancellationToken, vm.AddResident);
            return View(nameof(Index), invalidVm);
        }

        TempData["ManagementResidentsSuccess"] = T("ResidentAddedSuccessfully", "Resident added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    private async Task<(IActionResult? response, CompanyResidentsAuthorizedContext? context)> AuthorizeAsync(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (Challenge(), null);
        }

        var auth = await _residentAccessService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        if (auth.CompanyNotFound)
        {
            return (NotFound(), null);
        }

        if (auth.IsForbidden || auth.Context == null)
        {
            return (Forbid(), null);
        }

        return (null, auth.Context);
    }

    private async Task<ResidentsPageViewModel> BuildPageViewModelAsync(
        CompanyResidentsAuthorizedContext context,
        CancellationToken cancellationToken,
        AddManagementResidentViewModel? addResidentOverride = null)
    {
        var listResult = await _companyResidentService.ListAsync(context, cancellationToken);
        var title = T("Residents", "Residents");

        return new ResidentsPageViewModel
        {
            PageShell = await BuildManagementPageShellAsync(title, title, context.CompanySlug, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            Residents = listResult.Residents
                .Select(x => new ManagementResidentListItemViewModel
                {
                    ResidentId = x.ResidentId,
                    FullName = x.FullName,
                    IdCode = x.IdCode,
                    PreferredLanguage = x.PreferredLanguage,
                    IsActive = x.IsActive
                })
                .ToList(),
            AddResident = addResidentOverride ?? new AddManagementResidentViewModel()
        };
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
