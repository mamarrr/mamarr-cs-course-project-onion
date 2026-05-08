using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Contacts;
using App.BLL.Mappers.Contacts;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Contacts;

public class ContactWriter
{
    private readonly IAppUOW _uow;
    private readonly ContactBllDtoMapper _mapper = new();

    public ContactWriter(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<ContactBllDto>> StageCreateAsync(
        Guid managementCompanyId,
        ContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, managementCompanyId, null, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ContactBllDto>(validation.Errors);
        }

        var normalized = NormalizeForCompany(dto, managementCompanyId);
        normalized.Id = Guid.Empty;

        var mapped = _mapper.Map(normalized);
        if (mapped is null)
        {
            return Result.Fail<ContactBllDto>("Entity mapping failed.");
        }

        normalized.Id = _uow.Contacts.Add(mapped);
        return Result.Ok(normalized);
    }

    public async Task<Result> ValidateAsync(
        ContactBllDto dto,
        Guid managementCompanyId,
        Guid? exceptContactId,
        CancellationToken cancellationToken = default)
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
        else if (!await _uow.Lookups.ContactTypeExistsAsync(dto.ContactTypeId, cancellationToken))
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

        var normalized = NormalizeForCompany(dto, managementCompanyId);
        var duplicateExists = await _uow.Contacts.DuplicateValueExistsAsync(
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

    public ContactBllDto NormalizeForCompany(ContactBllDto dto, Guid managementCompanyId)
    {
        return new ContactBllDto
        {
            Id = dto.Id,
            ManagementCompanyId = managementCompanyId,
            ContactTypeId = dto.ContactTypeId,
            ContactValue = dto.ContactValue.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };
    }

    private static string RequiredField(string fieldName)
    {
        return App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName);
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
