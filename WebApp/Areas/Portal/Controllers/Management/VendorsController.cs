using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Vendors;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/vendors")]
public class VendorsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public VendorsController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.ManagementVendors)]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var route = BuildCompanyRoute(companySlug);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildIndexViewModelAsync(route, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(string companySlug, CancellationToken cancellationToken)
    {
        var route = BuildCompanyRoute(companySlug);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildFormViewModelAsync(route, null, new VendorFormViewModel(), cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string companySlug,
        VendorFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildCompanyRoute(companySlug);
        if (route is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildFormViewModelAsync(route, null, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var result = await _bll.Vendors.CreateAndGetProfileAsync(
            route,
            ToBllDto(vm.Form),
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "Form");
            var invalidPage = await BuildFormViewModelAsync(route, null, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorAddedSuccessfully", "Vendor added successfully.");
        return RedirectToAction(nameof(Details), new { companySlug, vendorId = result.Value.Id });
    }

    [HttpGet("{vendorId:guid}", Name = PortalRouteNames.ManagementVendorDetails)]
    public async Task<IActionResult> Details(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.GetProfileAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        return View(ToDetailsViewModel(
            result.Value,
            await BuildChromeAsync(result.Value.CompanySlug, result.Value.CompanyName, result.Value.Name, cancellationToken)));
    }

    [HttpGet("{vendorId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.GetProfileAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        var page = await BuildFormViewModelAsync(
            route,
            vendorId,
            new VendorFormViewModel
            {
                Name = result.Value.Name,
                RegistryCode = result.Value.RegistryCode,
                Notes = result.Value.Notes
            },
            cancellationToken);

        return page.response ?? View(page.model);
    }

    [HttpPost("{vendorId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid vendorId,
        VendorFormPageViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildFormViewModelAsync(route, vendorId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var result = await _bll.Vendors.UpdateAndGetProfileAsync(
            route,
            ToBllDto(vm.Form),
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "Form");
            var invalidPage = await BuildFormViewModelAsync(route, vendorId, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorUpdatedSuccessfully", "Vendor updated successfully.");
        return RedirectToAction(nameof(Details), new { companySlug, vendorId });
    }

    [HttpGet("{vendorId:guid}/delete")]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.GetProfileAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToMvcErrorResult(result.Errors);
        }

        return View(await ToDeleteViewModelAsync(result.Value, cancellationToken));
    }

    [HttpPost("{vendorId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid vendorId,
        VendorDeleteViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var profile = await _bll.Vendors.GetProfileAsync(route, cancellationToken);
        if (profile.IsFailed)
        {
            return ToMvcErrorResult(profile.Errors);
        }

        if (!ModelState.IsValid)
        {
            return View(await ToDeleteViewModelAsync(profile.Value, cancellationToken, vm.ConfirmationRegistryCode));
        }

        var result = await _bll.Vendors.DeleteAsync(
            route,
            vm.ConfirmationRegistryCode,
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, null);
            return View(await ToDeleteViewModelAsync(profile.Value, cancellationToken, vm.ConfirmationRegistryCode));
        }

        TempData["ManagementVendorsSuccess"] = T("VendorDeletedSuccessfully", "Vendor deleted successfully.");
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    private async Task<(IActionResult? response, VendorIndexViewModel model)> BuildIndexViewModelAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken)
    {
        var company = await _bll.Vendors.ResolveCompanyWorkspaceAsync(route, cancellationToken);
        if (company.IsFailed)
        {
            return (ToMvcErrorResult(company.Errors), new VendorIndexViewModel());
        }

        var vendors = await _bll.Vendors.ListForCompanyAsync(route, cancellationToken);
        if (vendors.IsFailed)
        {
            return (ToMvcErrorResult(vendors.Errors), new VendorIndexViewModel());
        }

        return (null, new VendorIndexViewModel
        {
            AppChrome = await BuildChromeAsync(company.Value.CompanySlug, company.Value.CompanyName, UiText.Vendors, cancellationToken),
            CompanySlug = company.Value.CompanySlug,
            CompanyName = company.Value.CompanyName,
            Vendors = vendors.Value.Select(ToListItem).ToList()
        });
    }

    private async Task<(IActionResult? response, VendorFormPageViewModel model)> BuildFormViewModelAsync(
        ManagementCompanyRoute route,
        Guid? vendorId,
        VendorFormViewModel form,
        CancellationToken cancellationToken)
    {
        var company = await _bll.Vendors.ResolveCompanyWorkspaceAsync(route, cancellationToken);
        if (company.IsFailed)
        {
            return (ToMvcErrorResult(company.Errors), new VendorFormPageViewModel());
        }

        return (null, new VendorFormPageViewModel
        {
            AppChrome = await BuildChromeAsync(company.Value.CompanySlug, company.Value.CompanyName, UiText.Vendors, cancellationToken),
            CompanySlug = company.Value.CompanySlug,
            CompanyName = company.Value.CompanyName,
            VendorId = vendorId,
            Form = form
        });
    }

    private async Task<AppChromeViewModel> BuildChromeAsync(
        string companySlug,
        string companyName,
        string pageTitle,
        CancellationToken cancellationToken)
    {
        return await _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = pageTitle,
                ActiveSection = Sections.Vendors,
                ManagementCompanySlug = companySlug,
                ManagementCompanyName = companyName,
                CurrentLevel = WorkspaceLevel.ManagementCompany
            },
            cancellationToken);
    }

    private static VendorListItemViewModel ToListItem(VendorListItemModel vendor)
    {
        return new VendorListItemViewModel
        {
            VendorId = vendor.VendorId,
            Name = vendor.Name,
            RegistryCode = vendor.RegistryCode,
            Notes = vendor.Notes,
            CreatedAt = vendor.CreatedAt,
            ActiveCategoryCount = vendor.ActiveCategoryCount,
            AssignedTicketCount = vendor.AssignedTicketCount,
            ContactCount = vendor.ContactCount
        };
    }

    private static VendorDetailsViewModel ToDetailsViewModel(
        VendorProfileModel vendor,
        AppChromeViewModel chrome)
    {
        return new VendorDetailsViewModel
        {
            AppChrome = chrome,
            CompanySlug = vendor.CompanySlug,
            CompanyName = vendor.CompanyName,
            VendorId = vendor.Id,
            Name = vendor.Name,
            RegistryCode = vendor.RegistryCode,
            Notes = vendor.Notes,
            CreatedAt = vendor.CreatedAt,
            ActiveCategoryCount = vendor.ActiveCategoryCount,
            AssignedTicketCount = vendor.AssignedTicketCount,
            ContactCount = vendor.ContactCount,
            ScheduledWorkCount = vendor.ScheduledWorkCount
        };
    }

    private async Task<VendorDeleteViewModel> ToDeleteViewModelAsync(
        VendorProfileModel vendor,
        CancellationToken cancellationToken,
        string? confirmationRegistryCode = null)
    {
        return new VendorDeleteViewModel
        {
            AppChrome = await BuildChromeAsync(vendor.CompanySlug, vendor.CompanyName, T("DeleteVendor", "Delete vendor"), cancellationToken),
            CompanySlug = vendor.CompanySlug,
            CompanyName = vendor.CompanyName,
            VendorId = vendor.Id,
            Name = vendor.Name,
            RegistryCode = vendor.RegistryCode,
            ConfirmationRegistryCode = confirmationRegistryCode ?? string.Empty
        };
    }

    private static VendorBllDto ToBllDto(VendorFormViewModel form)
    {
        return new VendorBllDto
        {
            Name = form.Name,
            RegistryCode = form.RegistryCode,
            Notes = form.Notes
        };
    }

    private ManagementCompanyRoute? BuildCompanyRoute(string companySlug)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new ManagementCompanyRoute { AppUserId = appUserId.Value, CompanySlug = companySlug };
    }

    private VendorRoute? BuildVendorRoute(string companySlug, Guid vendorId)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new VendorRoute { AppUserId = appUserId.Value, CompanySlug = companySlug, VendorId = vendorId };
    }

    private void AddErrorsToModelState(IEnumerable<IError> errors, string? prefix)
    {
        foreach (var validation in errors.OfType<ValidationAppError>())
        {
            foreach (var failure in validation.Failures)
            {
                var key = string.IsNullOrWhiteSpace(prefix)
                    ? failure.PropertyName
                    : $"{prefix}.{failure.PropertyName}";
                if (failure.PropertyName == "ConfirmationRegistryCode")
                {
                    key = failure.PropertyName;
                }

                ModelState.AddModelError(key, failure.ErrorMessage);
            }
        }

        foreach (var error in errors.Where(error => error is not ValidationAppError))
        {
            ModelState.AddModelError(string.Empty, error.Message);
        }
    }

    private static bool HasAccessError(IEnumerable<IError> errors)
    {
        return errors.Any(error => error is UnauthorizedError or NotFoundError or ForbiddenError);
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

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
