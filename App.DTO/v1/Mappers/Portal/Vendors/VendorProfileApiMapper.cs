using App.BLL.DTO.Vendors.Models;
using App.DTO.v1.Portal.Vendors;

namespace App.DTO.v1.Mappers.Portal.Vendors;

public sealed class VendorProfileApiMapper
{
    public VendorProfileDto Map(VendorProfileModel model)
    {
        return new VendorProfileDto
        {
            Id = model.Id,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            Notes = model.Notes,
            CreatedAt = model.CreatedAt,
            ActiveCategoryCount = model.ActiveCategoryCount,
            AssignedTicketCount = model.AssignedTicketCount,
            ContactCount = model.ContactCount,
            ScheduledWorkCount = model.ScheduledWorkCount,
            Path = VendorPath(model.CompanySlug, model.Id)
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
