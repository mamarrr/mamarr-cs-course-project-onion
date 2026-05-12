using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class ResidentContact_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ResidentContact_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddsConfirmsPrimaryAndRemovesContact()
    {
        var resident = await CreateResidentAsync("resident-contact");
        var (emailTypeId, phoneTypeId) = await ContactTypeIdsAsync();

        Guid emailAssignmentId;
        using (var addEmailScope = _factory.Services.CreateScope())
        {
            var bll = addEmailScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var result = await bll.Residents.AddContactAsync(
                ResidentRoute(resident.IdCode),
                new ResidentContactBllDto
                {
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                    Confirmed = false,
                    IsPrimary = true
                },
                new ContactBllDto
                {
                    ContactTypeId = emailTypeId,
                    ContactValue = $"{resident.IdCode.ToLowerInvariant()}@test.ee",
                    Notes = "Email contact"
                });

            result.IsSuccess.Should().BeTrue();
            var email = result.Value.Contacts.Single(contact => contact.ContactTypeCode == "EMAIL");
            email.IsPrimary.Should().BeTrue();
            email.Confirmed.Should().BeFalse();
            emailAssignmentId = email.ResidentContactId;
        }

        Guid phoneAssignmentId;
        using (var addPhoneScope = _factory.Services.CreateScope())
        {
            var bll = addPhoneScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var result = await bll.Residents.AddContactAsync(
                ResidentRoute(resident.IdCode),
                new ResidentContactBllDto
                {
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                    Confirmed = true,
                    IsPrimary = true
                },
                new ContactBllDto
                {
                    ContactTypeId = phoneTypeId,
                    ContactValue = $"+3725{Guid.NewGuid():N}"[..12],
                    Notes = "Phone contact"
                });

            result.IsSuccess.Should().BeTrue();
            result.Value.Contacts.Single(contact => contact.ResidentContactId == emailAssignmentId).IsPrimary.Should().BeFalse();
            var phone = result.Value.Contacts.Single(contact => contact.ContactTypeCode == "PHONE");
            phone.IsPrimary.Should().BeTrue();
            phoneAssignmentId = phone.ResidentContactId;
        }

        using (var confirmScope = _factory.Services.CreateScope())
        {
            var bll = confirmScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var confirmed = await bll.Residents.ConfirmContactAsync(ResidentContactRoute(resident.IdCode, emailAssignmentId));

            confirmed.IsSuccess.Should().BeTrue();
        }

        using (var primaryScope = _factory.Services.CreateScope())
        {
            var bll = primaryScope.ServiceProvider.GetRequiredService<IAppBLL>();

            var primary = await bll.Residents.SetPrimaryContactAsync(ResidentContactRoute(resident.IdCode, emailAssignmentId));
            var contacts = await bll.Residents.ListContactsAsync(ResidentRoute(resident.IdCode));

            primary.IsSuccess.Should().BeTrue();
            contacts.Value.Contacts.Single(contact => contact.ResidentContactId == emailAssignmentId).Confirmed.Should().BeTrue();
            contacts.Value.Contacts.Single(contact => contact.ResidentContactId == emailAssignmentId).IsPrimary.Should().BeTrue();
            contacts.Value.Contacts.Single(contact => contact.ResidentContactId == phoneAssignmentId).IsPrimary.Should().BeFalse();
        }

        using var removeScope = _factory.Services.CreateScope();
        var removeBll = removeScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var removed = await removeBll.Residents.RemoveContactAsync(ResidentContactRoute(resident.IdCode, phoneAssignmentId));
        var afterRemove = await removeBll.Residents.ListContactsAsync(ResidentRoute(resident.IdCode));

        removed.IsSuccess.Should().BeTrue();
        afterRemove.Value.Contacts.Should().NotContain(contact => contact.ResidentContactId == phoneAssignmentId);
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

    private async Task<(Guid EmailTypeId, Guid PhoneTypeId)> ContactTypeIdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<App.DAL.EF.AppDbContext>();

        var ids = await db.ContactTypes
            .AsNoTracking()
            .Where(type => type.Code == "EMAIL" || type.Code == "PHONE")
            .Select(type => new { type.Code, type.Id })
            .ToListAsync();

        return (
            ids.Single(type => type.Code == "EMAIL").Id,
            ids.Single(type => type.Code == "PHONE").Id);
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

    private static ResidentContactRoute ResidentContactRoute(string residentIdCode, Guid residentContactId)
    {
        return new ResidentContactRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            ResidentIdCode = residentIdCode,
            ResidentContactId = residentContactId
        };
    }

    private sealed record ResidentSeed(Guid ResidentId, string IdCode, string FirstName, string LastName);
}
