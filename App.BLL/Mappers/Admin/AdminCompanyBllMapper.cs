using App.BLL.DTO.Admin.Companies;
using App.DAL.DTO.Admin.Companies;

namespace App.BLL.Mappers.Admin;

public class AdminCompanyBllMapper
{
    public AdminCompanySearchDalDto Map(AdminCompanySearchDto dto)
    {
        return new AdminCompanySearchDalDto
        {
            SearchText = dto.SearchText,
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            Slug = dto.Slug
        };
    }

    public AdminCompanyListItemDto Map(AdminCompanyListItemDalDto dto)
    {
        return new AdminCompanyListItemDto
        {
            Id = dto.Id,
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            Slug = dto.Slug,
            Email = dto.Email,
            CreatedAt = dto.CreatedAt,
            UsersCount = dto.UsersCount,
            OpenTicketsCount = dto.OpenTicketsCount
        };
    }

    public AdminCompanyDetailsDto Map(AdminCompanyDetailsDalDto dto)
    {
        return new AdminCompanyDetailsDto
        {
            Id = dto.Id,
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            Slug = dto.Slug,
            Email = dto.Email,
            CreatedAt = dto.CreatedAt,
            UsersCount = dto.UsersCount,
            OpenTicketsCount = dto.OpenTicketsCount,
            VatNumber = dto.VatNumber,
            Phone = dto.Phone,
            Address = dto.Address,
            CustomersCount = dto.CustomersCount,
            PropertiesCount = dto.PropertiesCount,
            UnitsCount = dto.UnitsCount,
            ResidentsCount = dto.ResidentsCount,
            TicketsCount = dto.TicketsCount,
            VendorsCount = dto.VendorsCount,
            PendingJoinRequestsCount = dto.PendingJoinRequestsCount
        };
    }

    public AdminCompanyUpdateDalDto Map(AdminCompanyUpdateDto dto)
    {
        return new AdminCompanyUpdateDalDto
        {
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            VatNumber = dto.VatNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            Slug = dto.Slug
        };
    }
}
