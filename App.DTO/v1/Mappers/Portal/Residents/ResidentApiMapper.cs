using App.BLL.DTO.Residents;
using App.DTO.v1.Portal.Residents;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Residents;

public sealed class ResidentApiMapper :
    IBaseMapper<CreateResidentDto, ResidentBllDto>,
    IBaseMapper<UpdateResidentProfileDto, ResidentBllDto>
{
    CreateResidentDto? IBaseMapper<CreateResidentDto, ResidentBllDto>.Map(ResidentBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateResidentDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage
            };
    }

    ResidentBllDto? IBaseMapper<CreateResidentDto, ResidentBllDto>.Map(CreateResidentDto? entity)
    {
        return entity is null
            ? null
            : new ResidentBllDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage
            };
    }

    UpdateResidentProfileDto? IBaseMapper<UpdateResidentProfileDto, ResidentBllDto>.Map(ResidentBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateResidentProfileDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage
            };
    }

    ResidentBllDto? IBaseMapper<UpdateResidentProfileDto, ResidentBllDto>.Map(UpdateResidentProfileDto? entity)
    {
        return entity is null
            ? null
            : new ResidentBllDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage
            };
    }
}
