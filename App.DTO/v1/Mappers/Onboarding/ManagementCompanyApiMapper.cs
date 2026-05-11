using App.BLL.DTO.ManagementCompanies;
using App.DTO.v1.Onboarding;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Onboarding;

public class ManagementCompanyApiMapper
    : IBaseMapper<CreateManagementCompanyDto, ManagementCompanyBllDto>
{
    public CreateManagementCompanyDto? Map(ManagementCompanyBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateManagementCompanyDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                VatNumber = entity.VatNumber,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address
            };
    }

    public ManagementCompanyBllDto? Map(CreateManagementCompanyDto? entity)
    {
        return entity is null
            ? null
            : new ManagementCompanyBllDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                VatNumber = entity.VatNumber ?? string.Empty,
                Email = entity.Email,
                Phone = entity.Phone ?? string.Empty,
                Address = entity.Address
            };
    }
}
