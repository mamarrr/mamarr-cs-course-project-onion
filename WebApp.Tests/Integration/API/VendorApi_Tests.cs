using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Portal.VendorContacts;
using App.DTO.v1.Portal.Vendors;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class VendorApi_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VendorApi_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task VendorApi_CreateListUpdateDelete_Workflow()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        using var systemAdmin = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.SystemAdmin);
        var registryCode = UniqueCode("VEND");

        var unauthorized = await anonymous.GetAsync(VendorsPath());
        var forbidden = await systemAdmin.GetAsync(VendorsPath());
        var invalid = await owner.PostAsJsonAsync(VendorsPath(), new VendorRequestDto
        {
            Name = "",
            RegistryCode = registryCode,
            Notes = "Invalid vendor"
        });
        var created = await owner.PostAsJsonAsync(VendorsPath(), new VendorRequestDto
        {
            Name = "API Vendor",
            RegistryCode = registryCode,
            Notes = "API vendor notes"
        });
        var createdProfile = await created.Content.ReadFromJsonAsync<VendorProfileDto>();
        var list = await owner.GetFromJsonAsync<List<VendorListItemDto>>(VendorsPath());
        var updated = await owner.PutAsJsonAsync(VendorPath(createdProfile!.Id), new VendorRequestDto
        {
            Name = "API Vendor Updated",
            RegistryCode = registryCode,
            Notes = "Updated API vendor notes"
        });
        var updatedProfile = await updated.Content.ReadFromJsonAsync<VendorProfileDto>();
        var wrongDelete = await owner.SendAsync(JsonRequest(
            HttpMethod.Delete,
            VendorPath(updatedProfile!.Id),
            new DeleteVendorDto { ConfirmationRegistryCode = "wrong-code" }));
        var deleted = await owner.SendAsync(JsonRequest(
            HttpMethod.Delete,
            VendorPath(updatedProfile.Id),
            new DeleteVendorDto { ConfirmationRegistryCode = registryCode }));
        var afterDelete = await owner.GetAsync(VendorPath(updatedProfile.Id));

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        createdProfile.RegistryCode.Should().Be(registryCode);
        list.Should().Contain(vendor => vendor.VendorId == createdProfile.Id);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedProfile.Name.Should().Be("API Vendor Updated");
        updatedProfile.Notes.Should().Be("Updated API vendor notes");
        wrongDelete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VendorCategoriesApi_AssignDuplicateUpdateAndRemove_Workflow()
    {
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var vendor = await CreateVendorAsync(owner, "category");

        var list = await owner.GetAsync(VendorCategoriesPath(vendor.Id));
        var categoryList = await list.Content.ReadFromJsonAsync<VendorCategoryAssignmentListDto>();
        var assigned = await owner.PostAsJsonAsync(VendorCategoriesPath(vendor.Id), new AssignVendorCategoryDto
        {
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            Notes = "API category"
        });
        var assignedList = await assigned.Content.ReadFromJsonAsync<VendorCategoryAssignmentListDto>();
        var duplicate = await owner.PostAsJsonAsync(VendorCategoriesPath(vendor.Id), new AssignVendorCategoryDto
        {
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            Notes = "Duplicate API category"
        });
        var updated = await owner.PutAsJsonAsync(VendorCategoryPath(vendor.Id, TestTenants.TicketCategoryReferencedId), new UpdateVendorCategoryDto
        {
            Notes = "Updated API category"
        });
        var updatedList = await updated.Content.ReadFromJsonAsync<VendorCategoryAssignmentListDto>();
        var removed = await owner.DeleteAsync(VendorCategoryPath(vendor.Id, TestTenants.TicketCategoryReferencedId));
        var afterRemove = await owner.GetFromJsonAsync<VendorCategoryAssignmentListDto>(VendorCategoriesPath(vendor.Id));

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        categoryList.Should().NotBeNull();
        categoryList!.AvailableCategories.Should().Contain(category => category.Id == TestTenants.TicketCategoryReferencedId);
        assigned.StatusCode.Should().Be(HttpStatusCode.OK);
        assignedList!.Assignments.Should().ContainSingle(assignment =>
            assignment.TicketCategoryId == TestTenants.TicketCategoryReferencedId
            && assignment.Notes == "API category");
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedList!.Assignments.Should().ContainSingle(assignment =>
            assignment.TicketCategoryId == TestTenants.TicketCategoryReferencedId
            && assignment.Notes == "Updated API category");
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterRemove!.Assignments.Should().NotContain(assignment =>
            assignment.TicketCategoryId == TestTenants.TicketCategoryReferencedId);
    }

    [Fact]
    public async Task VendorContactsApi_CreateUpdateConfirmPrimaryAndDelete_Workflow()
    {
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var vendor = await CreateVendorAsync(owner, "contact");
        var contacts = await owner.GetAsync(VendorContactsPath(vendor.Id));
        var contactList = await contacts.Content.ReadFromJsonAsync<VendorContactListDto>();
        var emailType = contactList!.ContactTypeOptions.Single(type => type.Code == "EMAIL");

        var invalid = await owner.PostAsJsonAsync(VendorContactsPath(vendor.Id), new CreateAndAttachVendorContactDto
        {
            ContactTypeId = emailType.Id,
            ContactValue = "",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Confirmed = false,
            IsPrimary = true,
            FullName = "Invalid Contact",
            RoleTitle = "Coordinator"
        });
        var created = await owner.PostAsJsonAsync(VendorContactsPath(vendor.Id), new CreateAndAttachVendorContactDto
        {
            ContactTypeId = emailType.Id,
            ContactValue = $"{vendor.RegistryCode.ToLowerInvariant()}@vendor.test.ee",
            ContactNotes = "API vendor contact",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Confirmed = false,
            IsPrimary = true,
            FullName = "Vendor API Contact",
            RoleTitle = "Coordinator"
        });
        var createdList = await created.Content.ReadFromJsonAsync<VendorContactListDto>();
        var contact = createdList!.Contacts.Single(item => item.ContactTypeCode == "EMAIL");
        var edit = await owner.GetAsync(VendorContactPath(vendor.Id, contact.VendorContactId));
        var updated = await owner.PutAsJsonAsync(VendorContactPath(vendor.Id, contact.VendorContactId), new VendorContactAssignmentDto
        {
            ContactId = contact.ContactId,
            ValidFrom = contact.ValidFrom,
            Confirmed = false,
            IsPrimary = false,
            FullName = "Vendor API Contact Updated",
            RoleTitle = "Dispatcher"
        });
        var updatedList = await updated.Content.ReadFromJsonAsync<VendorContactListDto>();
        var confirmed = await owner.PostAsync($"{VendorContactPath(vendor.Id, contact.VendorContactId)}/confirm", null);
        var confirmedList = await confirmed.Content.ReadFromJsonAsync<VendorContactListDto>();
        var primary = await owner.PostAsync($"{VendorContactPath(vendor.Id, contact.VendorContactId)}/set-primary", null);
        var primaryList = await primary.Content.ReadFromJsonAsync<VendorContactListDto>();
        var deleted = await owner.DeleteAsync(VendorContactPath(vendor.Id, contact.VendorContactId));
        var deletedList = await deleted.Content.ReadFromJsonAsync<VendorContactListDto>();

        contacts.StatusCode.Should().Be(HttpStatusCode.OK);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        contact.IsPrimary.Should().BeTrue();
        contact.Confirmed.Should().BeFalse();
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedList!.Contacts.Single(item => item.VendorContactId == contact.VendorContactId).RoleTitle.Should().Be("Dispatcher");
        confirmed.StatusCode.Should().Be(HttpStatusCode.OK);
        confirmedList!.Contacts.Single(item => item.VendorContactId == contact.VendorContactId).Confirmed.Should().BeTrue();
        primary.StatusCode.Should().Be(HttpStatusCode.OK);
        primaryList!.Contacts.Single(item => item.VendorContactId == contact.VendorContactId).IsPrimary.Should().BeTrue();
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedList!.Contacts.Should().NotContain(item => item.VendorContactId == contact.VendorContactId);
    }

    private static async Task<VendorProfileDto> CreateVendorAsync(HttpClient client, string suffix)
    {
        var created = await client.PostAsJsonAsync(VendorsPath(), new VendorRequestDto
        {
            Name = $"API Vendor {suffix}",
            RegistryCode = UniqueCode("VEND"),
            Notes = "API vendor helper"
        });
        var profile = await created.Content.ReadFromJsonAsync<VendorProfileDto>();

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

    private static string VendorsPath()
    {
        return $"{CompanyPath()}/vendors";
    }

    private static string VendorPath(Guid vendorId)
    {
        return $"{VendorsPath()}/{vendorId:D}";
    }

    private static string VendorCategoriesPath(Guid vendorId)
    {
        return $"{VendorPath(vendorId)}/categories";
    }

    private static string VendorCategoryPath(Guid vendorId, Guid ticketCategoryId)
    {
        return $"{VendorCategoriesPath(vendorId)}/{ticketCategoryId:D}";
    }

    private static string VendorContactsPath(Guid vendorId)
    {
        return $"{VendorPath(vendorId)}/contacts";
    }

    private static string VendorContactPath(Guid vendorId, Guid vendorContactId)
    {
        return $"{VendorContactsPath(vendorId)}/{vendorContactId:D}";
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
