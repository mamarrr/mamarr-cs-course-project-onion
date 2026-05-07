using App.DAL.DTO.Admin.Companies;
using App.Domain;

namespace App.DAL.EF.Mappers.Admin;

public class AdminCompanyDalMapper
{
    public AdminCompanyListItemDalDto MapListItem(ManagementCompany company, int usersCount, int openTicketsCount)
    {
        return new AdminCompanyListItemDalDto
        {
            Id = company.Id,
            Name = company.Name,
            RegistryCode = company.RegistryCode,
            Slug = company.Slug,
            Email = company.Email,
            CreatedAt = company.CreatedAt,
            UsersCount = usersCount,
            OpenTicketsCount = openTicketsCount
        };
    }

    public AdminCompanyDetailsDalDto MapDetails(
        ManagementCompany company,
        int usersCount,
        int openTicketsCount,
        int customersCount,
        int propertiesCount,
        int unitsCount,
        int residentsCount,
        int ticketsCount,
        int vendorsCount,
        int pendingJoinRequestsCount)
    {
        return new AdminCompanyDetailsDalDto
        {
            Id = company.Id,
            Name = company.Name,
            RegistryCode = company.RegistryCode,
            Slug = company.Slug,
            Email = company.Email,
            CreatedAt = company.CreatedAt,
            UsersCount = usersCount,
            OpenTicketsCount = openTicketsCount,
            VatNumber = company.VatNumber,
            Phone = company.Phone,
            Address = company.Address,
            CustomersCount = customersCount,
            PropertiesCount = propertiesCount,
            UnitsCount = unitsCount,
            ResidentsCount = residentsCount,
            TicketsCount = ticketsCount,
            VendorsCount = vendorsCount,
            PendingJoinRequestsCount = pendingJoinRequestsCount
        };
    }
}
