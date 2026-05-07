using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents;
using App.BLL.DTO.Residents.Models;
using App.Resources.Views;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.ResidentContacts;

namespace WebApp.Areas.Portal.Controllers.Resident;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/r/{residentIdCode}/contacts")]
public class ResidentContactsController : Controller
{
    private const string IndexView = "~/Areas/Portal/Views/Management/ResidentContacts/Index.cshtml";
    private const string EditView = "~/Areas/Portal/Views/Management/ResidentContacts/Edit.cshtml";
    private const string DeleteView = "~/Areas/Portal/Views/Management/ResidentContacts/Delete.cshtml";

    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public ResidentContactsController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.ResidentContacts)]
    public async Task<IActionResult> Index(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentRoute(companySlug, residentIdCode);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildIndexPageAsync(
            route,
            new ResidentContactAttachExistingFormViewModel(),
            new ResidentContactCreateFormViewModel(),
            cancellationToken);

        return page.response ?? View(IndexView, page.model);
    }

    [HttpPost("attach")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AttachExisting(
        string companySlug,
        string residentIdCode,
        ResidentContactIndexViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentRoute(companySlug, residentIdCode);
        if (route is null)
        {
            return Challenge();
        }

        ClearModelStatePrefix(nameof(ResidentContactIndexViewModel.NewForm));
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildIndexPageAsync(route, vm.ExistingForm, new ResidentContactCreateFormViewModel(), cancellationToken);
            return invalidPage.response ?? View(IndexView, invalidPage.model);
        }

        var result = await _bll.Residents.AddContactAsync(
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
            var invalidPage = await BuildIndexPageAsync(route, vm.ExistingForm, new ResidentContactCreateFormViewModel(), cancellationToken);
            return invalidPage.response ?? View(IndexView, invalidPage.model);
        }

        TempData["ManagementResidentsSuccess"] = T("ResidentContactAddedSuccessfully", "Resident contact added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAndAttach(
        string companySlug,
        string residentIdCode,
        ResidentContactIndexViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentRoute(companySlug, residentIdCode);
        if (route is null)
        {
            return Challenge();
        }

        ClearModelStatePrefix(nameof(ResidentContactIndexViewModel.ExistingForm));
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildIndexPageAsync(route, new ResidentContactAttachExistingFormViewModel(), vm.NewForm, cancellationToken);
            return invalidPage.response ?? View(IndexView, invalidPage.model);
        }

        var result = await _bll.Residents.AddContactAsync(
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
            var invalidPage = await BuildIndexPageAsync(route, new ResidentContactAttachExistingFormViewModel(), vm.NewForm, cancellationToken);
            return invalidPage.response ?? View(IndexView, invalidPage.model);
        }

        TempData["ManagementResidentsSuccess"] = T("ResidentContactAddedSuccessfully", "Resident contact added successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    [HttpGet("{residentContactId:guid}/edit")]
    public async Task<IActionResult> Edit(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentContactRoute(companySlug, residentIdCode, residentContactId);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildEditPageAsync(route, null, cancellationToken);
        return page.response ?? View(EditView, page.model);
    }

    [HttpPost("{residentContactId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        ResidentContactEditViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentContactRoute(companySlug, residentIdCode, residentContactId);
        if (route is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildEditPageAsync(route, vm.Form, cancellationToken);
            return invalidPage.response ?? View(EditView, invalidPage.model);
        }

        var result = await _bll.Residents.UpdateContactAsync(
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
            return invalidPage.response ?? View(EditView, invalidPage.model);
        }

        TempData["ManagementResidentsSuccess"] = T("ResidentContactUpdatedSuccessfully", "Resident contact updated successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    [HttpGet("{residentContactId:guid}/delete")]
    public async Task<IActionResult> Delete(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentContactRoute(companySlug, residentIdCode, residentContactId);
        if (route is null)
        {
            return Challenge();
        }

        var page = await BuildDeletePageAsync(route, cancellationToken);
        return page.response ?? View(DeleteView, page.model);
    }

    [HttpPost("{residentContactId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        ResidentContactDeleteViewModel vm,
        CancellationToken cancellationToken)
    {
        var route = BuildResidentContactRoute(companySlug, residentIdCode, residentContactId);
        if (route is null)
        {
            return Challenge();
        }

        var result = await _bll.Residents.RemoveContactAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            if (HasAccessError(result.Errors))
            {
                return ToMvcErrorResult(result.Errors);
            }

            TempData["ManagementResidentsError"] = result.Errors.FirstOrDefault()?.Message
                                                   ?? T("UnableToRemoveResidentContact", "Unable to remove resident contact.");
            return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
        }

        TempData["ManagementResidentsSuccess"] = T("ResidentContactRemovedSuccessfully", "Resident contact removed successfully.");
        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    [HttpPost("{residentContactId:guid}/set-primary")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimary(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.SetPrimaryContactAsync(route, cancellationToken),
            T("ResidentPrimaryContactUpdatedSuccessfully", "Primary contact updated successfully."));
    }

    [HttpPost("{residentContactId:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.ConfirmContactAsync(route, cancellationToken),
            T("ResidentContactConfirmedSuccessfully", "Resident contact confirmed successfully."));
    }

    [HttpPost("{residentContactId:guid}/unconfirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unconfirm(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.UnconfirmContactAsync(route, cancellationToken),
            T("ResidentContactUnconfirmedSuccessfully", "Resident contact unconfirmed successfully."));
    }

    private async Task<IActionResult> RunContactCommandAsync(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        Func<ResidentContactRoute, Task<Result>> command,
        string successMessage)
    {
        var route = BuildResidentContactRoute(companySlug, residentIdCode, residentContactId);
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

            TempData["ManagementResidentsError"] = result.Errors.FirstOrDefault()?.Message
                                                   ?? T("UnableToUpdateResidentContact", "Unable to update resident contact.");
        }
        else
        {
            TempData["ManagementResidentsSuccess"] = successMessage;
        }

        return RedirectToAction(nameof(Index), new { companySlug, residentIdCode });
    }

    private async Task<(IActionResult? response, ResidentContactIndexViewModel? model)> BuildIndexPageAsync(
        ResidentRoute route,
        ResidentContactAttachExistingFormViewModel existingForm,
        ResidentContactCreateFormViewModel newForm,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Residents.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        return (null, await ToIndexViewModelAsync(result.Value, existingForm, newForm, cancellationToken));
    }

    private async Task<(IActionResult? response, ResidentContactEditViewModel? model)> BuildEditPageAsync(
        ResidentContactRoute route,
        ResidentContactMetadataFormViewModel? postedForm,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Residents.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var contact = result.Value.Contacts.FirstOrDefault(item => item.ResidentContactId == route.ResidentContactId);
        if (contact is null)
        {
            return (NotFound(), null);
        }

        var form = postedForm ?? new ResidentContactMetadataFormViewModel
        {
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo,
            Confirmed = contact.Confirmed,
            IsPrimary = contact.IsPrimary
        };

        return (null, new ResidentContactEditViewModel
        {
            AppChrome = await BuildChromeAsync(
                result.Value.CompanySlug,
                result.Value.CompanyName,
                result.Value.ResidentIdCode,
                result.Value.ResidentName,
                T("EditResidentContact", "Edit resident contact"),
                cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            ResidentIdCode = result.Value.ResidentIdCode,
            ResidentContactId = contact.ResidentContactId,
            ContactId = contact.ContactId,
            ResidentName = result.Value.ResidentName,
            ContactLabel = ContactLabel(contact.ContactTypeLabel, contact.ContactValue),
            Form = form
        });
    }

    private async Task<(IActionResult? response, ResidentContactDeleteViewModel? model)> BuildDeletePageAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _bll.Residents.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return (ToMvcErrorResult(result.Errors), null);
        }

        var contact = result.Value.Contacts.FirstOrDefault(item => item.ResidentContactId == route.ResidentContactId);
        if (contact is null)
        {
            return (NotFound(), null);
        }

        return (null, new ResidentContactDeleteViewModel
        {
            AppChrome = await BuildChromeAsync(
                result.Value.CompanySlug,
                result.Value.CompanyName,
                result.Value.ResidentIdCode,
                result.Value.ResidentName,
                T("RemoveResidentContact", "Remove resident contact"),
                cancellationToken),
            CompanySlug = result.Value.CompanySlug,
            CompanyName = result.Value.CompanyName,
            ResidentIdCode = result.Value.ResidentIdCode,
            ResidentContactId = contact.ResidentContactId,
            ResidentName = result.Value.ResidentName,
            ContactLabel = ContactLabel(contact.ContactTypeLabel, contact.ContactValue)
        });
    }

    private async Task<ResidentContactIndexViewModel> ToIndexViewModelAsync(
        ResidentContactListModel model,
        ResidentContactAttachExistingFormViewModel existingForm,
        ResidentContactCreateFormViewModel newForm,
        CancellationToken cancellationToken)
    {
        return new ResidentContactIndexViewModel
        {
            AppChrome = await BuildChromeAsync(
                model.CompanySlug,
                model.CompanyName,
                model.ResidentIdCode,
                model.ResidentName,
                T("ResidentContacts", "Resident contacts"),
                cancellationToken),
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            ResidentIdCode = model.ResidentIdCode,
            ResidentName = model.ResidentName,
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
        string residentIdCode,
        string residentName,
        string pageTitle,
        CancellationToken cancellationToken)
    {
        var residentDisplayName = string.IsNullOrWhiteSpace(residentName)
            ? residentIdCode
            : residentName;

        return await _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = pageTitle,
                ActiveSection = Sections.Contacts,
                ManagementCompanySlug = companySlug,
                ManagementCompanyName = companyName,
                ResidentIdCode = residentIdCode,
                ResidentDisplayName = residentDisplayName,
                ResidentSupportingText = string.IsNullOrWhiteSpace(residentName) ? null : residentIdCode,
                CurrentLevel = WorkspaceLevel.Resident
            },
            cancellationToken);
    }

    private static IReadOnlyList<SelectListItem> ExistingContactOptions(
        ResidentContactListModel model,
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

    private static ResidentContactAssignmentViewModel ToAssignmentViewModel(
        ResidentContactAssignmentModel contact)
    {
        return new ResidentContactAssignmentViewModel
        {
            ResidentContactId = contact.ResidentContactId,
            ContactId = contact.ContactId,
            ContactTypeLabel = contact.ContactTypeLabel,
            ContactValue = contact.ContactValue,
            IsPrimary = contact.IsPrimary,
            Confirmed = contact.Confirmed,
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo
        };
    }

    private static ResidentContactBllDto ToBllDto(ResidentContactAttachExistingFormViewModel form)
    {
        var dto = ToBllDto((ResidentContactMetadataFormViewModel)form);
        dto.ContactId = form.ContactId;
        return dto;
    }

    private static ResidentContactBllDto ToBllDto(ResidentContactMetadataFormViewModel form)
    {
        return new ResidentContactBllDto
        {
            ValidFrom = form.ValidFrom,
            ValidTo = form.ValidTo,
            Confirmed = form.Confirmed,
            IsPrimary = form.IsPrimary
        };
    }

    private static ResidentContactBllDto ToBllDto(ResidentContactMetadataFormViewModel form, Guid contactId)
    {
        var dto = ToBllDto(form);
        dto.ContactId = contactId;
        return dto;
    }

    private static ContactBllDto ToContactBllDto(ResidentContactCreateFormViewModel form)
    {
        return new ContactBllDto
        {
            ContactTypeId = form.ContactTypeId,
            ContactValue = form.ContactValue,
            Notes = form.ContactNotes
        };
    }

    private ResidentRoute? BuildResidentRoute(string companySlug, string residentIdCode)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new ResidentRoute { AppUserId = appUserId.Value, CompanySlug = companySlug, ResidentIdCode = residentIdCode };
    }

    private ResidentContactRoute? BuildResidentContactRoute(
        string companySlug,
        string residentIdCode,
        Guid residentContactId)
    {
        var appUserId = _portalContextResolver.Resolve().AppUserId;
        return !appUserId.HasValue || appUserId.Value == Guid.Empty
            ? null
            : new ResidentContactRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                ResidentIdCode = residentIdCode,
                ResidentContactId = residentContactId
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
