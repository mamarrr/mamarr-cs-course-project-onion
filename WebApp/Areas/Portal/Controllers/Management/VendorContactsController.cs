using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.VendorContacts;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/vendors/{vendorId:guid}/contacts")]
public class VendorContactsController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public VendorContactsController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildIndexPageAsync(
            route,
            new VendorContactAttachExistingFormViewModel(),
            new VendorContactCreateFormViewModel(),
            cancellationToken);

        return page.response ?? View(page.model);
    }

    [HttpPost("attach")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AttachExisting(
        string companySlug,
        Guid vendorId,
        VendorContactIndexViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        ClearModelStatePrefix(nameof(VendorContactIndexViewModel.NewForm));
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildIndexPageAsync(route, vm.ExistingForm, new VendorContactCreateFormViewModel(), cancellationToken);
            return invalidPage.response ?? View(nameof(Index), invalidPage.model);
        }

        var result = await _bll.Vendors.AddContactAsync(
            route,
            ToBllDto(vm.ExistingForm),
            null,
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "ExistingForm");
            var invalidPage = await BuildIndexPageAsync(route, vm.ExistingForm, new VendorContactCreateFormViewModel(), cancellationToken);
            return invalidPage.response ?? View(nameof(Index), invalidPage.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorContactAddedSuccessfully", "Vendor contact added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAndAttach(
        string companySlug,
        Guid vendorId,
        VendorContactIndexViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return Challenge();
        }

        ClearModelStatePrefix(nameof(VendorContactIndexViewModel.ExistingForm));
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildIndexPageAsync(route, new VendorContactAttachExistingFormViewModel(), vm.NewForm, cancellationToken);
            return invalidPage.response ?? View(nameof(Index), invalidPage.model);
        }

        var result = await _bll.Vendors.AddContactAsync(
            route,
            ToBllDto(vm.NewForm),
            ToContactBllDto(vm.NewForm),
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "NewForm");
            var invalidPage = await BuildIndexPageAsync(route, new VendorContactAttachExistingFormViewModel(), vm.NewForm, cancellationToken);
            return invalidPage.response ?? View(nameof(Index), invalidPage.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorContactAddedSuccessfully", "Vendor contact added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    [HttpGet("{vendorContactId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildEditPageAsync(route, null, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("{vendorContactId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        VendorContactEditViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildEditPageAsync(route, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        var result = await _bll.Vendors.UpdateContactAsync(
            route,
            ToBllDto(vm.Form, vm.ContactId),
            cancellationToken);

        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            AddErrorsToModelState(result.Errors, "Form");
            var invalidPage = await BuildEditPageAsync(route, vm.Form, cancellationToken);
            return invalidPage.response ?? View(invalidPage.model);
        }

        TempData["ManagementVendorsSuccess"] = T("VendorContactUpdatedSuccessfully", "Vendor contact updated successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    [HttpGet("{vendorContactId:guid}/delete")]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildDeletePageAsync(route, cancellationToken);
        return page.response ?? View(page.model);
    }

    [HttpPost("{vendorContactId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        VendorContactDeleteViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Vendors.RemoveContactAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementVendorsError"] = result.Errors.FirstOrDefault()?.Message
                                                 ?? T("UnableToRemoveVendorContact", "Unable to remove vendor contact.");
            return RedirectToAction(nameof(Index), new { companySlug, vendorId });
        }

        TempData["ManagementVendorsSuccess"] = T("VendorContactRemovedSuccessfully", "Vendor contact removed successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    [HttpPost("{vendorContactId:guid}/set-primary")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimary(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.SetPrimaryContactAsync(route, cancellationToken),
            T("VendorPrimaryContactUpdatedSuccessfully", "Primary contact updated successfully."));
    }

    [HttpPost("{vendorContactId:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.ConfirmContactAsync(route, cancellationToken),
            T("VendorContactConfirmedSuccessfully", "Vendor contact confirmed successfully."));
    }

    [HttpPost("{vendorContactId:guid}/unconfirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unconfirm(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.UnconfirmContactAsync(route, cancellationToken),
            T("VendorContactUnconfirmedSuccessfully", "Vendor contact unconfirmed successfully."));
    }

    private async Task<IActionResult> RunContactCommandAsync(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        Func<VendorContactRoute, Task<Result>> command,
        string successMessage)
    {
        var route = BuildVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await command(route);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementVendorsError"] = result.Errors.FirstOrDefault()?.Message
                                                 ?? T("UnableToUpdateVendorContact", "Unable to update vendor contact.");
        }
        else
        {
            TempData["ManagementVendorsSuccess"] = successMessage;
        }

        return RedirectToAction(nameof(Index), new { companySlug, vendorId });
    }

    private async Task<(IActionResult? response, VendorContactIndexViewModel? model)> BuildIndexPageAsync(
        VendorRoute route,
        VendorContactAttachExistingFormViewModel existingForm,
        VendorContactCreateFormViewModel newForm,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Vendors.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        return (null, await ToIndexViewModelAsync(result.Value, existingForm, newForm, cancellationToken));
    }

    private async Task<(IActionResult? response, VendorContactEditViewModel? model)> BuildEditPageAsync(
        VendorContactRoute route,
        VendorContactMetadataFormViewModel? postedForm,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Vendors.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var contact = result.Value.Contacts.FirstOrDefault(item => item.VendorContactId == route.VendorContactId);
        if (contact is null)
        {
            return (NotFound(), null);
        }

        var form = postedForm ?? new VendorContactMetadataFormViewModel
        {
            FullName = contact.FullName,
            RoleTitle = contact.RoleTitle,
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo,
            Confirmed = contact.Confirmed,
            IsPrimary = contact.IsPrimary
        };

        return (null, new VendorContactEditViewModel
        {
            AppChrome = await BuildChromeAsync(
                result.Value.CompanySlug,
                result.Value.CompanyName,
                T("EditVendorContact", "Edit vendor contact"),
                cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            VendorId = result.Value.VendorId,
            VendorContactId = contact.VendorContactId,
            ContactId = contact.ContactId,
            VendorName = result.Value.VendorName,
            ContactLabel = ContactLabel(contact.ContactTypeLabel, contact.ContactValue),
            Form = form,
            ExistingContactOptions = ExistingContactOptions(result.Value, contact.ContactId)
        });
    }

    private async Task<(IActionResult? response, VendorContactDeleteViewModel? model)> BuildDeletePageAsync(
        VendorContactRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Vendors.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var contact = result.Value.Contacts.FirstOrDefault(item => item.VendorContactId == route.VendorContactId);
        if (contact is null)
        {
            return (NotFound(), null);
        }

        return (null, new VendorContactDeleteViewModel
        {
            AppChrome = await BuildChromeAsync(
                result.Value.CompanySlug,
                result.Value.CompanyName,
                T("RemoveVendorContact", "Remove vendor contact"),
                cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            VendorId = result.Value.VendorId,
            VendorContactId = contact.VendorContactId,
            VendorName = result.Value.VendorName,
            ContactLabel = ContactLabel(contact.ContactTypeLabel, contact.ContactValue)
        });
    }

    private async Task<VendorContactIndexViewModel> ToIndexViewModelAsync(
        VendorContactListModel model,
        VendorContactAttachExistingFormViewModel existingForm,
        VendorContactCreateFormViewModel newForm,
        CancellationToken cancellationToken)
    {
        return new VendorContactIndexViewModel
        {
            AppChrome = await BuildChromeAsync(
                model.CompanySlug,
                model.CompanyName,
                T("VendorContacts", "Vendor contacts"),
                cancellationToken),
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            ExistingForm = existingForm,
            NewForm = newForm,
            Contacts = model.Contacts.Select(ToAssignmentViewModel).ToList(),
            ExistingContactOptions = ExistingContactOptions(model, null),
            ContactTypeOptions = model.ContactTypes.Select(option => new SelectListItem
            {
                Value = option.Id.ToString(),
                Text = option.Label
            }).ToList()
        };
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

    private static IReadOnlyList<SelectListItem> ExistingContactOptions(
        VendorContactListModel model,
        Guid? includeContactId)
    {
        var linkedContactIds = model.Contacts
            .Where(contact => includeContactId == null || contact.ContactId != includeContactId.Value)
            .Select(contact => contact.ContactId)
            .ToHashSet();

        return model.ExistingContacts
            .Where(contact => includeContactId == contact.Id || !linkedContactIds.Contains(contact.Id))
            .Select(contact => new SelectListItem
            {
                Value = contact.Id.ToString(),
                Text = contact.ContactValue
            })
            .ToList();
    }

    private static VendorContactAssignmentViewModel ToAssignmentViewModel(
        VendorContactAssignmentModel contact)
    {
        return new VendorContactAssignmentViewModel
        {
            VendorContactId = contact.VendorContactId,
            ContactId = contact.ContactId,
            ContactTypeLabel = contact.ContactTypeLabel,
            ContactValue = contact.ContactValue,
            FullName = contact.FullName,
            RoleTitle = contact.RoleTitle,
            IsPrimary = contact.IsPrimary,
            Confirmed = contact.Confirmed,
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo
        };
    }

    private static VendorContactBllDto ToBllDto(VendorContactAttachExistingFormViewModel form)
    {
        var dto = ToBllDto((VendorContactMetadataFormViewModel)form);
        dto.ContactId = form.ContactId;
        return dto;
    }

    private static VendorContactBllDto ToBllDto(VendorContactMetadataFormViewModel form)
    {
        return new VendorContactBllDto
        {
            ValidFrom = form.ValidFrom,
            ValidTo = form.ValidTo,
            Confirmed = form.Confirmed,
            IsPrimary = form.IsPrimary,
            FullName = form.FullName,
            RoleTitle = form.RoleTitle
        };
    }

    private static VendorContactBllDto ToBllDto(VendorContactMetadataFormViewModel form, Guid contactId)
    {
        var dto = ToBllDto(form);
        dto.ContactId = contactId;
        return dto;
    }

    private static ContactBllDto ToContactBllDto(VendorContactCreateFormViewModel form)
    {
        return new ContactBllDto
        {
            ContactTypeId = form.ContactTypeId,
            ContactValue = form.ContactValue,
            Notes = form.ContactNotes
        };
    }

    private VendorRoute? BuildVendorRoute(string companySlug, Guid vendorId)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new VendorRoute { AppUserId = appUserId.Value, CompanySlug = companySlug, VendorId = vendorId };
    }

    private VendorContactRoute? BuildVendorContactRoute(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new VendorContactRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                VendorId = vendorId,
                VendorContactId = vendorContactId
            };
    }

    private void AddErrorsToModelState(IEnumerable<IError> errors, string? prefix)
    {
        foreach (var validation in errors.OfType<ValidationAppError>())
        {
            foreach (var failure in validation.Failures)
            {
                ModelState.AddModelError(
                    string.IsNullOrWhiteSpace(prefix) ? failure.PropertyName : $"{prefix}.{failure.PropertyName}",
                    failure.ErrorMessage);
            }
        }

        foreach (var error in errors.Where(error => error is not ValidationAppError))
        {
            ModelState.AddModelError(string.Empty, error.Message);
        }
    }

    private void ClearModelStatePrefix(string prefix)
    {
        foreach (var key in ModelState.Keys.Where(key => key.StartsWith(prefix + ".", StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(key);
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

    private static string ContactLabel(string typeLabel, string value)
    {
        return string.IsNullOrWhiteSpace(typeLabel) ? value : $"{typeLabel}: {value}";
    }

    private static string T(string key, string fallback)
    {
        return UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
