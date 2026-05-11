using App.BLL.DTO.Admin.Companies;
using App.BLL.Mappers.Admin;
using App.DAL.DTO.Admin.Companies;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.Mappers.BLL;

public class AdminCompanyBllMapper_Tests
{
    private readonly AdminCompanyBllMapper _mapper = new();

    [Fact]
    public void MapSearch_MapsAllFilters()
    {
        var dto = new AdminCompanySearchDto
        {
            SearchText = "company",
            Name = "Company A",
            RegistryCode = "REG-A",
            Slug = "company-a"
        };

        var dal = _mapper.Map(dto);

        dal.SearchText.Should().Be(dto.SearchText);
        dal.Name.Should().Be(dto.Name);
        dal.RegistryCode.Should().Be(dto.RegistryCode);
        dal.Slug.Should().Be(dto.Slug);
    }

    [Fact]
    public void MapListItem_MapsScalarsAndCounts()
    {
        var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var dto = _mapper.Map(new AdminCompanyListItemDalDto
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            Name = "Company A",
            RegistryCode = "REG-A",
            Slug = "company-a",
            Email = "company@test.ee",
            CreatedAt = createdAt,
            UsersCount = 3,
            OpenTicketsCount = 5
        });

        dto.Id.Should().Be(new Guid("10000000-0000-0000-0000-000000000001"));
        dto.Name.Should().Be("Company A");
        dto.RegistryCode.Should().Be("REG-A");
        dto.Slug.Should().Be("company-a");
        dto.Email.Should().Be("company@test.ee");
        dto.CreatedAt.Should().Be(createdAt);
        dto.UsersCount.Should().Be(3);
        dto.OpenTicketsCount.Should().Be(5);
    }

    [Fact]
    public void MapDetails_MapsAllDetailFields()
    {
        var dto = _mapper.Map(new AdminCompanyDetailsDalDto
        {
            Id = new Guid("10000000-0000-0000-0000-000000000001"),
            Name = "Company A",
            RegistryCode = "REG-A",
            Slug = "company-a",
            Email = "company@test.ee",
            CreatedAt = DateTime.UtcNow,
            UsersCount = 3,
            OpenTicketsCount = 5,
            VatNumber = "EE100000001",
            Phone = "+372",
            Address = "Street 1",
            CustomersCount = 7,
            PropertiesCount = 9,
            UnitsCount = 11,
            ResidentsCount = 13,
            TicketsCount = 15,
            VendorsCount = 17,
            PendingJoinRequestsCount = 19
        });

        dto.VatNumber.Should().Be("EE100000001");
        dto.Phone.Should().Be("+372");
        dto.Address.Should().Be("Street 1");
        dto.CustomersCount.Should().Be(7);
        dto.PropertiesCount.Should().Be(9);
        dto.UnitsCount.Should().Be(11);
        dto.ResidentsCount.Should().Be(13);
        dto.TicketsCount.Should().Be(15);
        dto.VendorsCount.Should().Be(17);
        dto.PendingJoinRequestsCount.Should().Be(19);
    }

    [Fact]
    public void MapUpdate_MapsEditableFields()
    {
        var dto = new AdminCompanyUpdateDto
        {
            Name = "Company A",
            RegistryCode = "REG-A",
            VatNumber = "EE100000001",
            Email = "company@test.ee",
            Phone = "+372",
            Address = "Street 1",
            Slug = "company-a"
        };

        var dal = _mapper.Map(dto);

        dal.Name.Should().Be(dto.Name);
        dal.RegistryCode.Should().Be(dto.RegistryCode);
        dal.VatNumber.Should().Be(dto.VatNumber);
        dal.Email.Should().Be(dto.Email);
        dal.Phone.Should().Be(dto.Phone);
        dal.Address.Should().Be(dto.Address);
        dal.Slug.Should().Be(dto.Slug);
    }
}
