using App.BLL.DTO.Contacts;
using App.DAL.DTO.Contacts;
using Base.Contracts;

namespace App.BLL.Mappers.Contacts;

public class ContactBllDtoMapper : IBaseMapper<ContactBllDto, ContactDalDto>
{
    public ContactBllDto? Map(ContactDalDto? entity)
    {
        if (entity is null) return null;

        return new ContactBllDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            ContactTypeId = entity.ContactTypeId,
            ContactValue = entity.ContactValue,
            Notes = entity.Notes
        };
    }

    public ContactDalDto? Map(ContactBllDto? entity)
    {
        if (entity is null) return null;

        return new ContactDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            ContactTypeId = entity.ContactTypeId,
            ContactValue = entity.ContactValue,
            Notes = entity.Notes
        };
    }
}

