
using App.DAL.DTO.Contacts;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Contacts;

public class ContactDalMapper : IBaseMapper<ContactDalDto, Contact>
{
    public ContactDalDto? Map(Contact? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ContactDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            ContactTypeId = entity.ContactTypeId,
            ContactValue = entity.ContactValue,
            CreatedAt = entity.CreatedAt,
            Notes = entity.Notes?.ToString()
        };
    }

    public Contact? Map(ContactDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Contact
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            ContactTypeId = entity.ContactTypeId,
            ContactValue = entity.ContactValue,
            CreatedAt = entity.CreatedAt,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim())
        };
    }
}
