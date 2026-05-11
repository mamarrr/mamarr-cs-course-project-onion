using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents;
using App.DTO.v1.Portal.Contacts;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Contacts;

public class ResidentContactApiMapper :
    IBaseMapper<ResidentContactAssignmentDto, ResidentContactBllDto>,
    IBaseMapper<CreateAndAttachResidentContactDto, ResidentContactBllDto>,
    IBaseMapper<CreateAndAttachResidentContactDto, ContactBllDto>
{
    public ResidentContactBllDto? Map(ResidentContactAssignmentDto? entity)
    {
        return entity is null
            ? null
            : new ResidentContactBllDto
            {
                ContactId = entity.ContactId,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary
            };
    }

    ResidentContactBllDto? IBaseMapper<CreateAndAttachResidentContactDto, ResidentContactBllDto>.Map(
        CreateAndAttachResidentContactDto? entity)
    {
        return entity is null
            ? null
            : new ResidentContactBllDto
            {
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary
            };
    }

    ContactBllDto? IBaseMapper<CreateAndAttachResidentContactDto, ContactBllDto>.Map(
        CreateAndAttachResidentContactDto? entity)
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

    ResidentContactAssignmentDto? IBaseMapper<ResidentContactAssignmentDto, ResidentContactBllDto>.Map(
        ResidentContactBllDto? entity)
    {
        return entity is null
            ? null
            : new ResidentContactAssignmentDto
            {
                ContactId = entity.ContactId,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary
            };
    }

    CreateAndAttachResidentContactDto? IBaseMapper<CreateAndAttachResidentContactDto, ResidentContactBllDto>.Map(
        ResidentContactBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateAndAttachResidentContactDto
            {
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary
            };
    }

    CreateAndAttachResidentContactDto? IBaseMapper<CreateAndAttachResidentContactDto, ContactBllDto>.Map(
        ContactBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateAndAttachResidentContactDto
            {
                ContactTypeId = entity.ContactTypeId,
                ContactValue = entity.ContactValue,
                ContactNotes = entity.Notes
            };
    }

}
