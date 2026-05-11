using App.DAL.EF.Mappers.Admin;
using App.Domain;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.Mappers.DAL;

public class AdminCompanyDalMapper_Tests
{
    private readonly AdminCompanyDalMapper _mapper = new();

    [Fact]
    public void MapListItem_MapsCompanyScalarsAndCounts()
    {
        var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var company = Company(createdAt);

        var dto = _mapper.MapListItem(company, usersCount: 3, openTicketsCount: 5);

        dto.Id.Should().Be(company.Id);
        dto.Name.Should().Be(company.Name);
        dto.RegistryCode.Should().Be(company.RegistryCode);
        dto.Slug.Should().Be(company.Slug);
        dto.Email.Should().Be(company.Email);
        dto.CreatedAt.Should().Be(createdAt);
        dto.UsersCount.Should().Be(3);
        dto.OpenTicketsCount.Should().Be(5);
    }

    [Fact]
    public void MapDetails_MapsCompanyScalarsAndAggregateCounts()
    {
        var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var company = Company(createdAt);

        var dto = _mapper.MapDetails(
            company,
            usersCount: 3,
            openTicketsCount: 5,
            customersCount: 7,
            propertiesCount: 9,
            unitsCount: 11,
            residentsCount: 13,
            ticketsCount: 15,
            vendorsCount: 17,
            pendingJoinRequestsCount: 19);

        dto.Id.Should().Be(company.Id);
        dto.Name.Should().Be(company.Name);
        dto.RegistryCode.Should().Be(company.RegistryCode);
        dto.Slug.Should().Be(company.Slug);
        dto.Email.Should().Be(company.Email);
        dto.CreatedAt.Should().Be(createdAt);
        dto.VatNumber.Should().Be(company.VatNumber);
        dto.Phone.Should().Be(company.Phone);
        dto.Address.Should().Be(company.Address);
        dto.UsersCount.Should().Be(3);
        dto.OpenTicketsCount.Should().Be(5);
        dto.CustomersCount.Should().Be(7);
        dto.PropertiesCount.Should().Be(9);
        dto.UnitsCount.Should().Be(11);
        dto.ResidentsCount.Should().Be(13);
        dto.TicketsCount.Should().Be(15);
        dto.VendorsCount.Should().Be(17);
        dto.PendingJoinRequestsCount.Should().Be(19);
    }

    private static ManagementCompany Company(DateTime createdAt)
    {
        return new ManagementCompany
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            Name = "Company A",
            RegistryCode = "REG-A",
            Slug = "company-a",
            VatNumber = "EE100000001",
            Email = "company-a@test.ee",
            Phone = "+372 5555 0001",
            Address = "Street 1",
            CreatedAt = createdAt
        };
    }
}
