using App.BLL.DTO.Tickets.Models;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using App.DTO.v1.Portal.Vendors;
using App.DTO.v1.Shared;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Vendors;

public class VendorCategoryApiMapper :
    IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>,
    IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>
{
    AssignVendorCategoryDto? IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>.Map(
        VendorTicketCategoryBllDto? entity)
    {
        return entity is null
            ? null
            : new AssignVendorCategoryDto
            {
                TicketCategoryId = entity.TicketCategoryId,
                Notes = entity.Notes
            };
    }

    VendorTicketCategoryBllDto? IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>.Map(
        AssignVendorCategoryDto? entity)
    {
        return entity is null
            ? null
            : new VendorTicketCategoryBllDto
            {
                TicketCategoryId = entity.TicketCategoryId,
                Notes = entity.Notes
            };
    }

    UpdateVendorCategoryDto? IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>.Map(
        VendorTicketCategoryBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateVendorCategoryDto
            {
                Notes = entity.Notes
            };
    }

    VendorTicketCategoryBllDto? IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>.Map(
        UpdateVendorCategoryDto? entity)
    {
        return entity is null
            ? null
            : new VendorTicketCategoryBllDto
            {
                Notes = entity.Notes
            };
    }

    public VendorCategoryAssignmentListDto Map(VendorCategoryAssignmentListModel model)
    {
        return new VendorCategoryAssignmentListDto
        {
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            VendorId = model.VendorId,
            VendorName = model.VendorName,
            Path = CategoryListPath(model.CompanySlug, model.VendorId),
            Assignments = model.Assignments
                .Select(assignment => MapAssignment(model.CompanySlug, assignment))
                .ToList(),
            AvailableCategories = model.AvailableCategories.Select(MapOption).ToList()
        };
    }

    private static VendorCategoryAssignmentDto MapAssignment(
        string companySlug,
        VendorCategoryAssignmentModel model)
    {
        return new VendorCategoryAssignmentDto
        {
            AssignmentId = model.AssignmentId,
            VendorId = model.VendorId,
            TicketCategoryId = model.TicketCategoryId,
            CategoryCode = model.CategoryCode,
            CategoryLabel = model.CategoryLabel,
            Notes = model.Notes,
            CreatedAt = model.CreatedAt,
            Path = CategoryPath(companySlug, model.VendorId, model.TicketCategoryId)
        };
    }

    private static LookupOptionDto MapOption(TicketOptionModel option)
    {
        return new LookupOptionDto
        {
            Id = option.Id,
            Code = option.Code,
            Label = option.Label
        };
    }

    private static string CategoryPath(string companySlug, Guid vendorId, Guid ticketCategoryId)
    {
        return $"{CategoryListPath(companySlug, vendorId)}/{ticketCategoryId:D}";
    }

    private static string CategoryListPath(string companySlug, Guid vendorId)
    {
        return $"{CompanyApiPath(companySlug)}/vendors/{vendorId:D}/categories";
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
