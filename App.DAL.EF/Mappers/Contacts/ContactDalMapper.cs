using App.Contracts.DAL.Contacts;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Contacts;

public class ContactDalMapper : IMapper<ContactDalDto, Contact>
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
            ContactValue = entity.ContactValue
        };
    }
}
