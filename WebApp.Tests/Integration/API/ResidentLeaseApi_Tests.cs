using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Common;
using App.DTO.v1.Portal.Contacts;
using App.DTO.v1.Portal.Leases;
using App.DTO.v1.Portal.Residents;
using App.DTO.v1.Shared;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class ResidentLeaseApi_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ResidentLeaseApi_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResidentApi_CreateListUpdateDelete_Workflow()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        using var systemAdmin = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.SystemAdmin);
        var idCode = UniqueCode("API-RES");

        var unauthorized = await anonymous.GetAsync(ResidentsPath());
        var forbidden = await systemAdmin.GetAsync(ResidentsPath());
        var invalid = await owner.PostAsJsonAsync(ResidentsPath(), new ResidentRequestDto
        {
            FirstName = "",
            LastName = "Invalid",
            IdCode = idCode,
            PreferredLanguage = "en"
        });
        var created = await owner.PostAsJsonAsync(ResidentsPath(), new ResidentRequestDto
        {
            FirstName = "API",
            LastName = "Resident",
            IdCode = idCode,
            PreferredLanguage = "en"
        });
        var createdProfile = await created.Content.ReadFromJsonAsync<ResidentProfileDto>();
        var list = await owner.GetFromJsonAsync<List<ResidentListItemDto>>(ResidentsPath());
        var updatedIdCode = $"{idCode}-U";
        var updated = await owner.PutAsJsonAsync(ResidentProfilePath(createdProfile!.ResidentIdCode), new ResidentRequestDto
        {
            FirstName = "Updated",
            LastName = "Resident",
            IdCode = updatedIdCode,
            PreferredLanguage = "et"
        });
        var updatedProfile = await updated.Content.ReadFromJsonAsync<ResidentProfileDto>();
        var wrongDelete = await owner.SendAsync(JsonRequest(
            HttpMethod.Delete,
            ResidentProfilePath(updatedProfile!.ResidentIdCode),
            new DeleteConfirmationDto { DeleteConfirmation = "wrong-code" }));
        var deleted = await owner.SendAsync(JsonRequest(
            HttpMethod.Delete,
            ResidentProfilePath(updatedProfile.ResidentIdCode),
            new DeleteConfirmationDto { DeleteConfirmation = updatedProfile.ResidentIdCode }));
        var deleteResult = await deleted.Content.ReadFromJsonAsync<CommandResultDto>();
        var afterDelete = await owner.GetAsync(ResidentProfilePath(updatedProfile.ResidentIdCode));

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        createdProfile.ResidentIdCode.Should().Be(idCode);
        list.Should().Contain(resident => resident.IdCode == idCode);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedProfile.ResidentIdCode.Should().Be(updatedIdCode);
        updatedProfile.PreferredLanguage.Should().Be("et");
        wrongDelete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResult.Should().NotBeNull();
        deleteResult!.Success.Should().BeTrue();
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResidentContactsApi_CreateUpdateConfirmPrimaryAndDelete_Workflow()
    {
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var resident = await CreateResidentAsync(owner, "contact");
        var contacts = await owner.GetAsync(ResidentContactsPath(resident.ResidentIdCode));
        var contactList = await contacts.Content.ReadFromJsonAsync<ResidentContactListDto>();
        var emailType = contactList!.ContactTypeOptions.Single(type => type.Code == "EMAIL");

        var invalid = await owner.PostAsJsonAsync(ResidentContactsPath(resident.ResidentIdCode), new CreateAndAttachResidentContactDto
        {
            ContactTypeId = emailType.ContactTypeId,
            ContactValue = "",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Confirmed = false,
            IsPrimary = true
        });
        var created = await owner.PostAsJsonAsync(ResidentContactsPath(resident.ResidentIdCode), new CreateAndAttachResidentContactDto
        {
            ContactTypeId = emailType.ContactTypeId,
            ContactValue = $"{resident.ResidentIdCode.ToLowerInvariant()}@test.ee",
            ContactNotes = "API contact",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Confirmed = false,
            IsPrimary = true
        });
        var createdList = await created.Content.ReadFromJsonAsync<ResidentContactListDto>();
        var contact = createdList!.Contacts.Single(item => item.ContactTypeCode == "EMAIL");
        var edit = await owner.GetAsync(ResidentContactPath(resident.ResidentIdCode, contact.ResidentContactId));
        var updated = await owner.PutAsJsonAsync(ResidentContactPath(resident.ResidentIdCode, contact.ResidentContactId), new ResidentContactAssignmentDto
        {
            ContactId = contact.ContactId,
            ValidFrom = contact.ValidFrom,
            Confirmed = false,
            IsPrimary = false
        });
        var updatedList = await updated.Content.ReadFromJsonAsync<ResidentContactListDto>();
        var confirmed = await owner.PostAsync($"{ResidentContactPath(resident.ResidentIdCode, contact.ResidentContactId)}/confirm", null);
        var confirmedList = await confirmed.Content.ReadFromJsonAsync<ResidentContactListDto>();
        var primary = await owner.PostAsync($"{ResidentContactPath(resident.ResidentIdCode, contact.ResidentContactId)}/set-primary", null);
        var primaryList = await primary.Content.ReadFromJsonAsync<ResidentContactListDto>();
        var deleted = await owner.DeleteAsync(ResidentContactPath(resident.ResidentIdCode, contact.ResidentContactId));
        var deletedList = await deleted.Content.ReadFromJsonAsync<ResidentContactListDto>();

        contacts.StatusCode.Should().Be(HttpStatusCode.OK);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        contact.IsPrimary.Should().BeTrue();
        contact.Confirmed.Should().BeFalse();
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedList!.Contacts.Single(item => item.ResidentContactId == contact.ResidentContactId).IsPrimary.Should().BeFalse();
        confirmed.StatusCode.Should().Be(HttpStatusCode.OK);
        confirmedList!.Contacts.Single(item => item.ResidentContactId == contact.ResidentContactId).Confirmed.Should().BeTrue();
        primary.StatusCode.Should().Be(HttpStatusCode.OK);
        primaryList!.Contacts.Single(item => item.ResidentContactId == contact.ResidentContactId).IsPrimary.Should().BeTrue();
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedList!.Contacts.Should().NotContain(item => item.ResidentContactId == contact.ResidentContactId);
    }

    [Fact]
    public async Task LeaseApi_CreateListUpdateDeleteFromResidentAndUnitRoutes()
    {
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var resident = await CreateResidentAsync(owner, "lease");
        var roles = await owner.GetFromJsonAsync<LeaseRoleOptionsDto>(ResidentLeaseRolesPath(resident.ResidentIdCode));
        var tenantRole = roles!.Roles.Single(role => role.Code == "TENANT");
        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30));

        var propertySearch = await owner.GetAsync($"{ResidentPropertySearchPath(resident.ResidentIdCode)}?searchTerm=Property%20A");
        var propertySearchDto = await propertySearch.Content.ReadFromJsonAsync<LeasePropertySearchResultDto>();
        var units = await owner.GetAsync(ResidentPropertyUnitsPath(resident.ResidentIdCode, TestTenants.PropertyAId));
        var unitOptions = await units.Content.ReadFromJsonAsync<LeaseUnitOptionsDto>();
        var invalid = await owner.PostAsJsonAsync(ResidentLeasesPath(resident.ResidentIdCode), new CreateResidentLeaseDto
        {
            UnitId = TestTenants.UnitAId,
            LeaseRoleId = tenantRole.LeaseRoleId,
            StartDate = start,
            EndDate = start.AddDays(-1)
        });
        var created = await owner.PostAsJsonAsync(ResidentLeasesPath(resident.ResidentIdCode), new CreateResidentLeaseDto
        {
            UnitId = TestTenants.UnitAId,
            LeaseRoleId = tenantRole.LeaseRoleId,
            StartDate = start,
            Notes = "API resident lease"
        });
        var createdLease = await created.Content.ReadFromJsonAsync<LeaseDto>();
        var byResident = await owner.GetFromJsonAsync<List<ResidentLeaseListItemDto>>(ResidentLeasesPath(resident.ResidentIdCode));
        var byUnit = await owner.GetFromJsonAsync<List<UnitLeaseListItemDto>>(UnitLeasesPath());
        var detailsByUnit = await owner.GetAsync(UnitLeasePath(createdLease!.LeaseId));
        var updated = await owner.PutAsJsonAsync(UnitLeasePath(createdLease.LeaseId), new UpdateLeaseDto
        {
            LeaseRoleId = tenantRole.LeaseRoleId,
            StartDate = start,
            EndDate = start.AddDays(20),
            Notes = "Updated API lease"
        });
        var updatedLease = await updated.Content.ReadFromJsonAsync<LeaseDto>();
        var deleted = await owner.DeleteAsync(ResidentLeasePath(resident.ResidentIdCode, createdLease.LeaseId));
        var deleteResult = await deleted.Content.ReadFromJsonAsync<CommandResultDto>();
        var afterDelete = await owner.GetAsync(ResidentLeasePath(resident.ResidentIdCode, createdLease.LeaseId));

        propertySearch.StatusCode.Should().Be(HttpStatusCode.OK);
        propertySearchDto!.Properties.Should().Contain(property => property.PropertyId == TestTenants.PropertyAId);
        units.StatusCode.Should().Be(HttpStatusCode.OK);
        unitOptions!.Units.Should().Contain(unit => unit.UnitId == TestTenants.UnitAId);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        createdLease.ResidentId.Should().Be(resident.ResidentId);
        createdLease.UnitId.Should().Be(TestTenants.UnitAId);
        byResident.Should().Contain(lease => lease.LeaseId == createdLease.LeaseId);
        byUnit.Should().Contain(lease => lease.LeaseId == createdLease.LeaseId && lease.ResidentId == resident.ResidentId);
        detailsByUnit.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedLease!.EndDate.Should().Be(start.AddDays(20));
        updatedLease.Notes.Should().Be("Updated API lease");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResult.Should().NotBeNull();
        deleteResult!.Success.Should().BeTrue();
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<ResidentProfileDto> CreateResidentAsync(HttpClient client, string suffix)
    {
        var created = await client.PostAsJsonAsync(ResidentsPath(), new ResidentRequestDto
        {
            FirstName = "API",
            LastName = $"Resident {suffix}",
            IdCode = UniqueCode("API-RES"),
            PreferredLanguage = "en"
        });
        var profile = await created.Content.ReadFromJsonAsync<ResidentProfileDto>();

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        profile.Should().NotBeNull();
        return profile!;
    }

    private static HttpRequestMessage JsonRequest<T>(HttpMethod method, string path, T body)
    {
        return new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
    }

    private static string CompanyPath()
    {
        return $"/api/v1/portal/companies/{TestTenants.CompanyASlug}";
    }

    private static string ResidentsPath()
    {
        return $"{CompanyPath()}/residents";
    }

    private static string ResidentProfilePath(string residentIdCode)
    {
        return $"{ResidentsPath()}/{residentIdCode}/profile";
    }

    private static string ResidentContactsPath(string residentIdCode)
    {
        return $"{ResidentsPath()}/{residentIdCode}/contacts";
    }

    private static string ResidentContactPath(string residentIdCode, Guid residentContactId)
    {
        return $"{ResidentContactsPath(residentIdCode)}/{residentContactId:D}";
    }

    private static string ResidentLeasesPath(string residentIdCode)
    {
        return $"{CompanyPath()}/residents/{residentIdCode}/leases";
    }

    private static string ResidentLeasePath(string residentIdCode, Guid leaseId)
    {
        return $"{ResidentLeasesPath(residentIdCode)}/{leaseId:D}";
    }

    private static string ResidentLeaseRolesPath(string residentIdCode)
    {
        return $"{ResidentLeasesPath(residentIdCode)}/roles";
    }

    private static string ResidentPropertySearchPath(string residentIdCode)
    {
        return $"{ResidentLeasesPath(residentIdCode)}/property-search";
    }

    private static string ResidentPropertyUnitsPath(string residentIdCode, Guid propertyId)
    {
        return $"{ResidentLeasesPath(residentIdCode)}/properties/{propertyId:D}/units";
    }

    private static string UnitLeasesPath()
    {
        return $"{CompanyPath()}/customers/customer-a/properties/property-a/units/a-101/leases";
    }

    private static string UnitLeasePath(Guid leaseId)
    {
        return $"{UnitLeasesPath()}/{leaseId:D}";
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
    }
}
