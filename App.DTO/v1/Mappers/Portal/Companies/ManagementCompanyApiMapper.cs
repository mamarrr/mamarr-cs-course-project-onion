using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.ManagementCompanies.Models;
using App.DTO.v1.Portal.Companies;

namespace App.DTO.v1.Mappers.Portal.Companies;

public sealed class ManagementCompanyApiMapper
{
    public ManagementCompanyProfileDto Map(CompanyProfileModel model)
    {
        return new ManagementCompanyProfileDto
        {
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            VatNumber = model.VatNumber,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address,
            Path = CompanyPath(model.CompanySlug),
            EditPath = $"{CompanyPath(model.CompanySlug)}/profile/edit"
        };
    }

    public ManagementCompanyBllDto Map(UpdateManagementCompanyDto dto)
    {
        return new ManagementCompanyBllDto
        {
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            VatNumber = dto.VatNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };
    }

    private static string CompanyPath(string companySlug) => $"/companies/{Uri.EscapeDataString(companySlug)}";
}
