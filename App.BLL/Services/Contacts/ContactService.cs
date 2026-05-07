using App.BLL.Contracts.Contacts;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.Mappers.Contacts;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Contacts;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Contacts;

public class ContactService :
    BaseService<ContactBllDto, ContactDalDto, IContactRepository, IAppUOW>,
    IContactService
{
    private static readonly HashSet<string> ReadAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private static readonly HashSet<string> WriteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "SUPPORT"
    };

    private static readonly HashSet<string> DeleteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    public ContactService(IAppUOW uow)
        : base(uow.Contacts, uow, new ContactBllDtoMapper())
    {
    }

    public async Task<Result<IReadOnlyList<ContactBllDto>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<IReadOnlyList<ContactBllDto>>(access.Errors);
        }

        var contacts = await ServiceUOW.Contacts.OptionsByCompanyAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<ContactBllDto>)contacts
            .Select(contact => Mapper.Map(contact)!)
            .ToList());
    }

    public async Task<Result<ContactBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        ContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ContactBllDto>(access.Errors);
        }

        var validation = await ValidateAsync(dto, access.Value.ManagementCompanyId, null, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ContactBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        normalized.Id = Guid.Empty;
        normalized.ManagementCompanyId = access.Value.ManagementCompanyId;

        return await AddAndFindCoreAsync(normalized, access.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<ContactBllDto>> UpdateAsync(
        ContactRoute route,
        ContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ContactBllDto>(access.Errors);
        }

        var existing = await ServiceUOW.Contacts.FindInCompanyAsync(
            route.ContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail<ContactBllDto>(new NotFoundError(T("ContactNotFound", "Contact was not found.")));
        }

        var validation = await ValidateAsync(dto, access.Value.ManagementCompanyId, route.ContactId, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ContactBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        normalized.Id = route.ContactId;
        normalized.ManagementCompanyId = access.Value.ManagementCompanyId;

        var updated = await base.UpdateAsync(normalized, access.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ContactBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result> DeleteAsync(
        ContactRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var exists = await ServiceUOW.Contacts.ExistsInCompanyAsync(
            route.ContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (!exists)
        {
            return Result.Fail(new NotFoundError(T("ContactNotFound", "Contact was not found.")));
        }

        var hasDependencies = await ServiceUOW.Contacts.HasDependenciesAsync(
            route.ContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (hasDependencies)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(route.ContactId, access.Value.ManagementCompanyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<CompanyAccessContext>> ResolveCompanyAccessAsync(
        ManagementCompanyRoute route,
        IReadOnlySet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail<CompanyAccessContext>(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail<CompanyAccessContext>(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail<CompanyAccessContext>(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        return roleCode is not null && allowedRoleCodes.Contains(roleCode)
            ? Result.Ok(new CompanyAccessContext(company.Id))
            : Result.Fail<CompanyAccessContext>(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
    }

    private async Task<Result> ValidateAsync(
        ContactBllDto dto,
        Guid managementCompanyId,
        Guid? exceptContactId,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailureModel>();

        if (dto.ContactTypeId == Guid.Empty)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.ContactTypeId),
                ErrorMessage = RequiredField(App.Resources.Views.UiText.Contacts)
            });
        }
        else if (!await ServiceUOW.Lookups.ContactTypeExistsAsync(dto.ContactTypeId, cancellationToken))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.ContactTypeId),
                ErrorMessage = T("InvalidContactType", "Selected contact type is invalid.")
            });
        }

        if (string.IsNullOrWhiteSpace(dto.ContactValue))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.ContactValue),
                ErrorMessage = RequiredField(App.Resources.Views.UiText.Contacts)
            });
        }
        else if (dto.ContactValue.Trim().Length > 255)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.ContactValue),
                ErrorMessage = T("ContactValueMaxLength", "Contact value must be 255 characters or fewer.")
            });
        }

        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Trim().Length > 4000)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.Notes),
                ErrorMessage = T("ContactNotesMaxLength", "Notes must be 4000 characters or fewer.")
            });
        }

        if (failures.Count > 0)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", failures));
        }

        var normalized = Normalize(dto);
        var duplicateExists = await ServiceUOW.Contacts.DuplicateValueExistsAsync(
            managementCompanyId,
            normalized.ContactTypeId,
            normalized.ContactValue,
            exceptContactId,
            cancellationToken);
        if (duplicateExists)
        {
            return Result.Fail(new ConflictError(T(
                "ContactAlreadyExists",
                "A contact with the same type and value already exists in this company.")));
        }

        return Result.Ok();
    }

    private static ContactBllDto Normalize(ContactBllDto dto)
    {
        return new ContactBllDto
        {
            Id = dto.Id,
            ManagementCompanyId = dto.ManagementCompanyId,
            ContactTypeId = dto.ContactTypeId,
            ContactValue = dto.ContactValue.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };
    }

    private static string RequiredField(string fieldName)
    {
        return App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName);
    }

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private sealed record CompanyAccessContext(Guid ManagementCompanyId);
}
