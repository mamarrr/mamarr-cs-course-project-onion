using App.BLL.DTO.Vendors.Models;
using App.DTO.v1.Portal.Vendors;

namespace App.DTO.v1.Mappers.Portal.Vendors;

public sealed class VendorListItemApiMapper
{
    public VendorListItemDto Map(VendorListItemModel model)
    {
        return new VendorListItemDto
        {
            VendorId = model.VendorId,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            CreatedAt = model.CreatedAt,
            ActiveCategoryCount = model.ActiveCategoryCount,
            AssignedTicketCount = model.AssignedTicketCount,
            ContactCount = model.ContactCount,
            Path = VendorPath(model.CompanySlug, model.VendorId)
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
