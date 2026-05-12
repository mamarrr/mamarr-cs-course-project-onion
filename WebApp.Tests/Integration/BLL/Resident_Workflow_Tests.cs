using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Residents;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Resident_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Resident_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUpdateAndListResident_Workflow()
    {
        var resident = await CreateResidentAsync("resident-list");

        using (var listScope = _factory.Services.CreateScope())
        {
            var bll = listScope.ServiceProvider.GetRequiredService<IAppBLL>();

            var list = await bll.Residents.ListForCompanyAsync(CompanyRoute());
            var profile = await bll.Residents.GetProfileAsync(ResidentRoute(resident.IdCode));

            list.Value.Residents.Should().Contain(item => item.ResidentId == resident.ResidentId);
            profile.Value.FirstName.Should().Be(resident.FirstName);
            profile.Value.LastName.Should().Be(resident.LastName);
        }

        using var updateScope = _factory.Services.CreateScope();
        var updateBll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var updated = await updateBll.Residents.UpdateAndGetProfileAsync(
            ResidentRoute(resident.IdCode),
            new ResidentBllDto
            {
                FirstName = "Updated",
                LastName = "Resident",
                IdCode = $"{resident.IdCode}-U",
                PreferredLanguage = "et"
            });

        updated.IsSuccess.Should().BeTrue();
        updated.Value.FirstName.Should().Be("Updated");
        updated.Value.ResidentIdCode.Should().Be($"{resident.IdCode}-U");
        updated.Value.PreferredLanguage.Should().Be("et");
    }

    private async Task<ResidentSeed> CreateResidentAsync(string suffix)
    {
        var idCode = $"BLL-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var firstName = $"Resident{suffix}"[..Math.Min($"Resident{suffix}".Length, 30)];

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var created = await bll.Residents.CreateAndGetProfileAsync(
            CompanyRoute(),
            new ResidentBllDto
            {
                FirstName = firstName,
                LastName = "Workflow",
                IdCode = idCode,
                PreferredLanguage = "en"
            });

        created.IsSuccess.Should().BeTrue();
        return new ResidentSeed(created.Value.ResidentId, created.Value.ResidentIdCode, firstName, "Workflow");
    }

    private static ManagementCompanyRoute CompanyRoute()
    {
        return new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        };
    }

    private static ResidentRoute ResidentRoute(string residentIdCode)
    {
        return new ResidentRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            ResidentIdCode = residentIdCode
        };
    }

    private sealed record ResidentSeed(Guid ResidentId, string IdCode, string FirstName, string LastName);
}
