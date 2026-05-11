using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Tickets.Models;
using App.DTO.v1.Portal.Contacts;

namespace App.DTO.v1.Mappers.Portal.Contacts;

public sealed class ResidentContactListApiMapper
{
    public ResidentContactListDto Map(ResidentContactListModel model)
    {
        return new ResidentContactListDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            ResidentId = model.ResidentId,
            ResidentIdCode = model.ResidentIdCode,
            ResidentName = model.ResidentName,
            Contacts = model.Contacts
                .Select(contact => MapContact(contact, model.CompanySlug, model.ResidentIdCode))
                .ToList(),
            ExistingContactOptions = MapExistingContactOptions(model.ExistingContacts),
            ContactTypeOptions = MapContactTypeOptions(model.ContactTypes)
        };
    }

    public ResidentContactEditDto MapEdit(
        ResidentContactListModel model,
        ResidentContactAssignmentModel contact)
    {
        return new ResidentContactEditDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            ResidentId = model.ResidentId,
            ResidentIdCode = model.ResidentIdCode,
            ResidentName = model.ResidentName,
            Contact = MapContact(contact, model.CompanySlug, model.ResidentIdCode),
            Form = new UpdateResidentContactDto
            {
                ContactId = contact.ContactId,
                ValidFrom = contact.ValidFrom,
                ValidTo = contact.ValidTo,
                Confirmed = contact.Confirmed,
                IsPrimary = contact.IsPrimary
            },
            ExistingContactOptions = MapExistingContactOptions(model.ExistingContacts),
            ContactTypeOptions = MapContactTypeOptions(model.ContactTypes)
        };
    }

    private static ResidentContactDto MapContact(
        ResidentContactAssignmentModel contact,
        string companySlug,
        string residentIdCode)
    {
        return new ResidentContactDto
        {
            ResidentContactId = contact.ResidentContactId,
            ResidentId = contact.ResidentId,
            ContactId = contact.ContactId,
            ContactTypeId = contact.ContactTypeId,
            ContactTypeCode = contact.ContactTypeCode,
            ContactTypeLabel = contact.ContactTypeLabel,
            ContactValue = contact.ContactValue,
            ContactNotes = contact.ContactNotes,
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo,
            Confirmed = contact.Confirmed,
            IsPrimary = contact.IsPrimary,
            CreatedAt = contact.CreatedAt,
            Path = BuildPath(companySlug, residentIdCode, contact.ResidentContactId)
        };
    }

    private static IReadOnlyList<ExistingContactOptionDto> MapExistingContactOptions(
        IReadOnlyList<ContactBllDto> contacts)
    {
        return contacts
            .Select(contact => new ExistingContactOptionDto
            {
                ContactId = contact.Id,
                ContactTypeId = contact.ContactTypeId,
                ContactValue = contact.ContactValue,
                Notes = contact.Notes
            })
            .ToList();
    }

    private static IReadOnlyList<ContactTypeOptionDto> MapContactTypeOptions(
        IReadOnlyList<TicketOptionModel> contactTypes)
    {
        return contactTypes
            .Select(contactType => new ContactTypeOptionDto
            {
                ContactTypeId = contactType.Id,
                Code = contactType.Code,
                Label = contactType.Label
            })
            .ToList();
    }

    private static string BuildPath(
        string companySlug,
        string residentIdCode,
        Guid residentContactId)
    {
        return $"/api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId}";
    }
}

