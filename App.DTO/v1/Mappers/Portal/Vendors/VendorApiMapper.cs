using App.BLL.DTO.Vendors;
using App.DTO.v1.Portal.Vendors;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Vendors;

public class VendorApiMapper :
    IBaseMapper<VendorRequestDto, VendorBllDto>
{
    public VendorRequestDto? Map(VendorBllDto? entity)
    {
        return entity is null
            ? null
            : new VendorRequestDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                Notes = entity.Notes
            };
    }

    public VendorBllDto? Map(VendorRequestDto? entity)
    {
        return entity is null ? null : MapCommand(entity.Name, entity.RegistryCode, entity.Notes);
    }

    public VendorDto Map(VendorBllDto dto, string companySlug)
    {
        return new VendorDto
        {
            VendorId = dto.Id,
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            Notes = dto.Notes,
            Path = VendorPath(companySlug, dto.Id)
        };
    }

    private static VendorBllDto MapCommand(string name, string registryCode, string notes)
    {
        return new VendorBllDto
        {
            Name = name,
            RegistryCode = registryCode,
            Notes = notes
        };
    }

    private static string VendorPath(string companySlug, Guid vendorId)
    {
        return $"{CompanyApiPath(companySlug)}/vendors/{vendorId:D}";
    }

    private static string CompanyApiPath(string companySlug)
    {
        return $"/api/v1/portal/companies/{Segment(companySlug)}";
    }

    private static string Segment(string value)
    {
        return Uri.EscapeDataString(value);
    }
}
