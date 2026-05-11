using App.BLL.DTO.Contacts;
using App.BLL.DTO.Tickets.Models;
using App.BLL.DTO.Vendors.Models;
using App.DTO.v1.Portal.VendorContacts;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Mappers.Portal.VendorContacts;

public class VendorContactListApiMapper
{
    public VendorContactListDto Map(VendorContactListModel model)
    {
        return new VendorContactListDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            Path = BuildListPath(model.CompanySlug, model.VendorId),
            Contacts = model.Contacts.Select(contact => MapAssignment(model.CompanySlug, contact)).ToList(),
            ExistingContactOptions = MapExistingContactOptions(model, null),
            ContactTypeOptions = model.ContactTypes.Select(MapContactTypeOption).ToList()
        };
    }

    public VendorContactEditModelDto? MapEditModel(VendorContactListModel model, Guid vendorContactId)
    {
        var contact = model.Contacts.FirstOrDefault(item => item.VendorContactId == vendorContactId);
        if (contact is null)
        {
            return null;
        }

        return new VendorContactEditModelDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            Contact = MapAssignment(model.CompanySlug, contact),
            ExistingContactOptions = MapExistingContactOptions(model, contact.ContactId),
            ContactTypeOptions = model.ContactTypes.Select(MapContactTypeOption).ToList()
        };
    }

    private static VendorContactDto MapAssignment(
        string companySlug,
        VendorContactAssignmentModel contact)
    {
        return new VendorContactDto
        {
            VendorContactId = contact.VendorContactId,
            VendorId = contact.VendorId,
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
            FullName = contact.FullName,
            RoleTitle = contact.RoleTitle,
            CreatedAt = contact.CreatedAt,
            Path = BuildContactPath(companySlug, contact.VendorId, contact.VendorContactId)
        };
    }

    private static IReadOnlyList<ExistingVendorContactOptionDto> MapExistingContactOptions(
        VendorContactListModel model,
        Guid? includeContactId)
    {
        var linkedContactIds = model.Contacts
            .Where(contact => includeContactId is null || contact.ContactId != includeContactId.Value)
            .Select(contact => contact.ContactId)
            .ToHashSet();

        return model.ExistingContacts
            .Where(contact => includeContactId == contact.Id || !linkedContactIds.Contains(contact.Id))
            .Select(MapExistingContactOption)
            .ToList();
    }

    private static ExistingVendorContactOptionDto MapExistingContactOption(ContactBllDto contact)
    {
        return new ExistingVendorContactOptionDto
        {
            ContactId = contact.Id,
            ContactTypeId = contact.ContactTypeId,
            ContactValue = contact.ContactValue,
            Notes = contact.Notes,
            Label = contact.ContactValue
        };
    }

    private static LookupOptionDto MapContactTypeOption(TicketOptionModel option)
    {
        return new LookupOptionDto
        {
            Id = option.Id,
            Label = option.Label,
            Code = option.Code
        };
    }

    private static string BuildListPath(string companySlug, Guid vendorId)
    {
        return $"/api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts";
    }

    private static string BuildContactPath(string companySlug, Guid vendorId, Guid vendorContactId)
    {
        return $"{BuildListPath(companySlug, vendorId)}/{vendorContactId}";
    }
}
