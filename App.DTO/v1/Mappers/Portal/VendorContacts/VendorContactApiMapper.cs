using App.BLL.DTO.Contacts;
using App.BLL.DTO.Vendors;
using App.DTO.v1.Portal.VendorContacts;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.VendorContacts;

public class VendorContactApiMapper :
    IBaseMapper<VendorContactAssignmentDto, VendorContactBllDto>,
    IBaseMapper<CreateAndAttachVendorContactDto, VendorContactBllDto>,
    IBaseMapper<CreateAndAttachVendorContactDto, ContactBllDto>
{
    public VendorContactAssignmentDto? Map(VendorContactBllDto? entity)
    {
        return entity is null
            ? null
            : new VendorContactAssignmentDto
            {
                ContactId = entity.ContactId,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary,
                FullName = entity.FullName,
                RoleTitle = entity.RoleTitle
            };
    }

    public VendorContactBllDto? Map(VendorContactAssignmentDto? entity)
    {
        return entity is null ? null : MapMetadata(entity, entity.ContactId);
    }

    CreateAndAttachVendorContactDto? IBaseMapper<CreateAndAttachVendorContactDto, VendorContactBllDto>.Map(
        VendorContactBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateAndAttachVendorContactDto
            {
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary,
                FullName = entity.FullName,
                RoleTitle = entity.RoleTitle
            };
    }

    VendorContactBllDto? IBaseMapper<CreateAndAttachVendorContactDto, VendorContactBllDto>.Map(
        CreateAndAttachVendorContactDto? entity)
    {
        return entity is null ? null : MapMetadata(entity, Guid.Empty);
    }

    CreateAndAttachVendorContactDto? IBaseMapper<CreateAndAttachVendorContactDto, ContactBllDto>.Map(
        ContactBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateAndAttachVendorContactDto
            {
                ContactTypeId = entity.ContactTypeId,
                ContactValue = entity.ContactValue,
                ContactNotes = entity.Notes
            };
    }

    ContactBllDto? IBaseMapper<CreateAndAttachVendorContactDto, ContactBllDto>.Map(
        CreateAndAttachVendorContactDto? entity)
    {
        return entity is null
            ? null
            : new ContactBllDto
            {
                ContactTypeId = entity.ContactTypeId,
                ContactValue = entity.ContactValue,
                Notes = entity.ContactNotes
            };
    }

    private static VendorContactBllDto MapMetadata(VendorContactMetadataDto dto, Guid contactId)
    {
        return new VendorContactBllDto
        {
            ContactId = contactId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Confirmed = dto.Confirmed,
            IsPrimary = dto.IsPrimary,
            FullName = dto.FullName,
            RoleTitle = dto.RoleTitle
        };
    }
}
