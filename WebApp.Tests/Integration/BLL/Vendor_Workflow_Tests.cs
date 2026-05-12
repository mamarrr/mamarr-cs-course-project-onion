using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Vendors;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Vendor_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Vendor_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListUpdateAndDeleteVendor_Workflow()
    {
        var vendor = await CreateVendorAsync("vendor-basic");

        using (var listScope = _factory.Services.CreateScope())
        {
            var bll = listScope.ServiceProvider.GetRequiredService<IAppBLL>();

            var list = await bll.Vendors.ListForCompanyAsync(CompanyRoute());
            var profile = await bll.Vendors.GetProfileAsync(VendorRoute(vendor.VendorId));

            list.Value.Should().Contain(item => item.VendorId == vendor.VendorId);
            profile.Value.Name.Should().Be(vendor.Name);
            profile.Value.RegistryCode.Should().Be(vendor.RegistryCode);
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.Vendors.UpdateAndGetProfileAsync(
                VendorRoute(vendor.VendorId),
                new VendorBllDto
                {
                    Name = "Updated Vendor",
                    RegistryCode = vendor.RegistryCode,
                    Notes = "Updated vendor notes"
                });

            updated.IsSuccess.Should().BeTrue();
            updated.Value.Name.Should().Be("Updated Vendor");
            updated.Value.Notes.Should().Be("Updated vendor notes");
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.Vendors.DeleteAsync(VendorRoute(vendor.VendorId), vendor.RegistryCode);
        var afterDelete = await deleteBll.Vendors.GetProfileAsync(VendorRoute(vendor.VendorId));

        deleted.IsSuccess.Should().BeTrue();
        afterDelete.ShouldFailWith<NotFoundError>();
    }

    [Fact]
    public async Task CategoryAssignmentWorkflow_DetectsDuplicatesAndSupportsUpdateRemove()
    {
        var vendor = await CreateVendorAsync("vendor-category");

        using (var assignScope = _factory.Services.CreateScope())
        {
            var bll = assignScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var assigned = await bll.Vendors.AssignCategoryAsync(
                VendorRoute(vendor.VendorId),
                new VendorTicketCategoryBllDto
                {
                    TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                    Notes = "Initial category note"
                });
            var duplicate = await bll.Vendors.AssignCategoryAsync(
                VendorRoute(vendor.VendorId),
                new VendorTicketCategoryBllDto
                {
                    TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                    Notes = "Duplicate category note"
                });

            assigned.IsSuccess.Should().BeTrue();
            assigned.Value.Assignments.Should().ContainSingle(assignment =>
                assignment.TicketCategoryId == TestTenants.TicketCategoryReferencedId
                && assignment.Notes == "Initial category note");
            duplicate.ShouldFailWith<ConflictError>();
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.Vendors.UpdateCategoryAssignmentAsync(
                VendorCategoryRoute(vendor.VendorId, TestTenants.TicketCategoryReferencedId),
                new VendorTicketCategoryBllDto
                {
                    Notes = "Updated category note"
                });

            updated.IsSuccess.Should().BeTrue();
            updated.Value.Assignments.Should().ContainSingle(assignment =>
                assignment.TicketCategoryId == TestTenants.TicketCategoryReferencedId
                && assignment.Notes == "Updated category note");
        }

        using var removeScope = _factory.Services.CreateScope();
        var removeBll = removeScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var removed = await removeBll.Vendors.RemoveCategoryAsync(
            VendorCategoryRoute(vendor.VendorId, TestTenants.TicketCategoryReferencedId));
        var afterRemove = await removeBll.Vendors.ListCategoryAssignmentsAsync(VendorRoute(vendor.VendorId));

        removed.IsSuccess.Should().BeTrue();
        afterRemove.Value.Assignments.Should().NotContain(assignment =>
            assignment.TicketCategoryId == TestTenants.TicketCategoryReferencedId);
    }

    [Fact]
    public async Task ContactWorkflow_AddsConfirmsPrimaryAndRemovesContact()
    {
        var vendor = await CreateVendorAsync("vendor-contact");
        var (emailTypeId, phoneTypeId) = await ContactTypeIdsAsync();

        Guid emailAssignmentId;
        using (var addEmailScope = _factory.Services.CreateScope())
        {
            var bll = addEmailScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var result = await bll.Vendors.AddContactAsync(
                VendorRoute(vendor.VendorId),
                new VendorContactBllDto
                {
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                    Confirmed = false,
                    IsPrimary = true,
                    FullName = "Vendor Email Contact",
                    RoleTitle = "Coordinator"
                },
                new ContactBllDto
                {
                    ContactTypeId = emailTypeId,
                    ContactValue = $"{vendor.RegistryCode.ToLowerInvariant()}@vendor.test.ee",
                    Notes = "Vendor email"
                });

            result.IsSuccess.Should().BeTrue();
            var email = result.Value.Contacts.Single(contact => contact.ContactTypeCode == "EMAIL");
            email.IsPrimary.Should().BeTrue();
            email.Confirmed.Should().BeFalse();
            emailAssignmentId = email.VendorContactId;
        }

        Guid phoneAssignmentId;
        using (var addPhoneScope = _factory.Services.CreateScope())
        {
            var bll = addPhoneScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var result = await bll.Vendors.AddContactAsync(
                VendorRoute(vendor.VendorId),
                new VendorContactBllDto
                {
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                    Confirmed = true,
                    IsPrimary = true,
                    FullName = "Vendor Phone Contact",
                    RoleTitle = "Dispatcher"
                },
                new ContactBllDto
                {
                    ContactTypeId = phoneTypeId,
                    ContactValue = $"+3726{Guid.NewGuid():N}"[..12],
                    Notes = "Vendor phone"
                });

            result.IsSuccess.Should().BeTrue();
            result.Value.Contacts.Single(contact => contact.VendorContactId == emailAssignmentId).IsPrimary.Should().BeFalse();
            var phone = result.Value.Contacts.Single(contact => contact.ContactTypeCode == "PHONE");
            phone.IsPrimary.Should().BeTrue();
            phoneAssignmentId = phone.VendorContactId;
        }

        using (var confirmScope = _factory.Services.CreateScope())
        {
            var bll = confirmScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var confirmed = await bll.Vendors.ConfirmContactAsync(VendorContactRoute(vendor.VendorId, emailAssignmentId));

            confirmed.IsSuccess.Should().BeTrue();
        }

        using (var primaryScope = _factory.Services.CreateScope())
        {
            var bll = primaryScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var primary = await bll.Vendors.SetPrimaryContactAsync(VendorContactRoute(vendor.VendorId, emailAssignmentId));
            var contacts = await bll.Vendors.ListContactsAsync(VendorRoute(vendor.VendorId));

            primary.IsSuccess.Should().BeTrue();
            contacts.Value.Contacts.Single(contact => contact.VendorContactId == emailAssignmentId).Confirmed.Should().BeTrue();
            contacts.Value.Contacts.Single(contact => contact.VendorContactId == emailAssignmentId).IsPrimary.Should().BeTrue();
            contacts.Value.Contacts.Single(contact => contact.VendorContactId == phoneAssignmentId).IsPrimary.Should().BeFalse();
        }

        using var removeScope = _factory.Services.CreateScope();
        var removeBll = removeScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var removed = await removeBll.Vendors.RemoveContactAsync(VendorContactRoute(vendor.VendorId, phoneAssignmentId));
        var afterRemove = await removeBll.Vendors.ListContactsAsync(VendorRoute(vendor.VendorId));

        removed.IsSuccess.Should().BeTrue();
        afterRemove.Value.Contacts.Should().NotContain(contact => contact.VendorContactId == phoneAssignmentId);
    }

    [Fact]
    public async Task DeleteIsBlockedWhileCategoryOrContactDependenciesExist()
    {
        var categoryVendor = await CreateVendorAsync("vendor-category-delete-blocked");
        var contactVendor = await CreateVendorAsync("vendor-contact-delete-blocked");
        var emailTypeId = (await ContactTypeIdsAsync()).EmailTypeId;

        using (var dependencyScope = _factory.Services.CreateScope())
        {
            var bll = dependencyScope.ServiceProvider.GetRequiredService<IAppBLL>();

            var category = await bll.Vendors.AssignCategoryAsync(
                VendorRoute(categoryVendor.VendorId),
                new VendorTicketCategoryBllDto { TicketCategoryId = TestTenants.TicketCategoryReferencedId });
            var contact = await bll.Vendors.AddContactAsync(
                VendorRoute(contactVendor.VendorId),
                new VendorContactBllDto
                {
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                    IsPrimary = true
                },
                new ContactBllDto
                {
                    ContactTypeId = emailTypeId,
                    ContactValue = $"{contactVendor.RegistryCode.ToLowerInvariant()}@vendor.test.ee"
                });

            category.IsSuccess.Should().BeTrue();
            contact.IsSuccess.Should().BeTrue();
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var categoryBlocked = await deleteBll.Vendors.DeleteAsync(
            VendorRoute(categoryVendor.VendorId),
            categoryVendor.RegistryCode);
        var contactBlocked = await deleteBll.Vendors.DeleteAsync(
            VendorRoute(contactVendor.VendorId),
            contactVendor.RegistryCode);

        categoryBlocked.ShouldFailWith<BusinessRuleError>();
        contactBlocked.ShouldFailWith<BusinessRuleError>();
    }

    [Fact]
    public async Task ValidationDuplicateRegistryAndMissingRouteFailuresAreReturned()
    {
        var vendor = await CreateVendorAsync("vendor-validation");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var invalid = await bll.Vendors.CreateAsync(CompanyRoute(), new VendorBllDto
        {
            Name = " ",
            RegistryCode = UniqueCode("VEND"),
            Notes = "Missing name"
        });
        var duplicate = await bll.Vendors.CreateAsync(CompanyRoute(), new VendorBllDto
        {
            Name = "Duplicate Vendor",
            RegistryCode = vendor.RegistryCode,
            Notes = "Duplicate registry"
        });
        var missing = await bll.Vendors.GetProfileAsync(VendorRoute(Guid.NewGuid()));

        invalid.ShouldFailWith<ValidationAppError>();
        duplicate.ShouldFailWith<ConflictError>();
        missing.ShouldFailWith<NotFoundError>();
    }

    private async Task<VendorSeed> CreateVendorAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var registryCode = UniqueCode("VEND");
        var name = $"Workflow Vendor {suffix}";

        var created = await bll.Vendors.CreateAndGetProfileAsync(
            CompanyRoute(),
            new VendorBllDto
            {
                Name = name,
                RegistryCode = registryCode,
                Notes = "Workflow vendor notes"
            });

        created.IsSuccess.Should().BeTrue();
        return new VendorSeed(created.Value.Id, created.Value.Name, created.Value.RegistryCode);
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

    private static VendorRoute VendorRoute(Guid vendorId)
    {
        return new VendorRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            VendorId = vendorId
        };
    }

    private static VendorCategoryRoute VendorCategoryRoute(Guid vendorId, Guid ticketCategoryId)
    {
        return new VendorCategoryRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            VendorId = vendorId,
            TicketCategoryId = ticketCategoryId
        };
    }

    private static VendorContactRoute VendorContactRoute(Guid vendorId, Guid vendorContactId)
    {
        return new VendorContactRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            VendorId = vendorId,
            VendorContactId = vendorContactId
        };
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record VendorSeed(Guid VendorId, string Name, string RegistryCode);
}
