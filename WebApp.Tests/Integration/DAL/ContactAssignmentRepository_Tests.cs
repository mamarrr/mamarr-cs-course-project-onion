using App.DAL.Contracts;
using App.DAL.DTO.Contacts;
using App.DAL.DTO.Lookups;
using App.DAL.DTO.Residents;
using App.DAL.DTO.Vendors;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class ContactAssignmentRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ContactAssignmentRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResidentContactRepository_ManagesScopedAssignmentsAndPrimaryFlag()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var contactId = await CreateContactAsync(uow, await ContactTypeIdAsync(db), "resident");
        var residentId = await CreateResidentAsync(uow);
        var assignmentId = Guid.NewGuid();

        uow.ResidentContacts.Add(new ResidentContactDalDto
        {
            Id = assignmentId,
            ResidentId = residentId,
            ContactId = contactId,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Confirmed = true,
            IsPrimary = true
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var all = (await uow.ResidentContacts.AllAsync(TestTenants.CompanyAId)).ToList();
        var byResident = await uow.ResidentContacts.AllByResidentAsync(residentId, TestTenants.CompanyAId);
        var found = await uow.ResidentContacts.FindInCompanyAsync(assignmentId, TestTenants.CompanyAId);
        var hasPrimary = await uow.ResidentContacts.HasPrimaryAsync(residentId, TestTenants.CompanyAId);
        var linked = await uow.ResidentContacts.ContactLinkedToResidentAsync(residentId, contactId, TestTenants.CompanyAId);

        all.Should().Contain(item => item.Id == assignmentId);
        byResident.Should().ContainSingle(item => item.Id == assignmentId);
        byResident[0].ContactValue.Should().Contain("@test.ee");
        found.Should().NotBeNull();
        hasPrimary.Should().BeTrue();
        linked.Should().BeTrue();

        await uow.ResidentContacts.ClearPrimaryAsync(residentId, TestTenants.CompanyAId);
        var primaryAfterClear = await uow.ResidentContacts.HasPrimaryAsync(residentId, TestTenants.CompanyAId);
        primaryAfterClear.Should().BeFalse();

        var deleted = await uow.ResidentContacts.DeleteInCompanyAsync(assignmentId, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);
        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task VendorContactRepository_ManagesScopedAssignmentsAndRoleTitle()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var contactId = await CreateContactAsync(uow, await ContactTypeIdAsync(db), "vendor");
        var assignmentId = Guid.NewGuid();

        uow.VendorContacts.Add(new VendorContactDalDto
        {
            Id = assignmentId,
            VendorId = TestTenants.VendorAId,
            ContactId = contactId,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Confirmed = true,
            IsPrimary = true,
            FullName = "Vendor Contact",
            RoleTitle = "Coordinator"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var byVendor = await uow.VendorContacts.AllByVendorAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var found = await uow.VendorContacts.FindInCompanyAsync(assignmentId, TestTenants.CompanyAId);
        var hasPrimary = await uow.VendorContacts.HasPrimaryAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var linked = await uow.VendorContacts.ContactLinkedToVendorAsync(TestTenants.VendorAId, contactId, TestTenants.CompanyAId);

        byVendor.Should().Contain(item => item.Id == assignmentId && item.RoleTitle == "Coordinator");
        found.Should().NotBeNull();
        hasPrimary.Should().BeTrue();
        linked.Should().BeTrue();

        await uow.VendorContacts.ClearPrimaryAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var primaryAfterClear = await uow.VendorContacts.HasPrimaryAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        primaryAfterClear.Should().BeFalse();

        var deleted = await uow.VendorContacts.DeleteInCompanyAsync(assignmentId, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);
        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task VendorTicketCategoryRepository_ManagesScopedAssignments()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var category = await uow.Lookups.CreateLookupItemAsync(LookupTable.TicketCategory, UniqueCode("CAT"), "Custom category");
        await uow.SaveChangesAsync(CancellationToken.None);
        var assignmentId = Guid.NewGuid();

        uow.VendorTicketCategories.Add(new VendorTicketCategoryDalDto
        {
            Id = assignmentId,
            VendorId = TestTenants.VendorAId,
            TicketCategoryId = category.Id,
            Notes = "Category note"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var byVendor = await uow.VendorTicketCategories.AllByVendorAsync(TestTenants.VendorAId, TestTenants.CompanyAId);
        var found = await uow.VendorTicketCategories.FindInCompanyAsync(assignmentId, TestTenants.CompanyAId);
        var foundByPair = await uow.VendorTicketCategories.FindByVendorCategoryInCompanyAsync(TestTenants.VendorAId, category.Id, TestTenants.CompanyAId);
        var exists = await uow.VendorTicketCategories.ExistsInCompanyAsync(TestTenants.VendorAId, category.Id, TestTenants.CompanyAId);

        byVendor.Should().Contain(item => item.Id == assignmentId && item.CategoryLabel == "Custom category");
        found.Should().NotBeNull();
        foundByPair.Should().NotBeNull();
        exists.Should().BeTrue();

        var deleted = await uow.VendorTicketCategories.DeleteAssignmentAsync(TestTenants.VendorAId, category.Id, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);
        deleted.Should().BeTrue();
    }

    private static async Task<Guid> CreateContactAsync(IAppUOW uow, Guid contactTypeId, string suffix)
    {
        var id = Guid.NewGuid();
        uow.Contacts.Add(new ContactDalDto
        {
            Id = id,
            ManagementCompanyId = TestTenants.CompanyAId,
            ContactTypeId = contactTypeId,
            ContactValue = $"{suffix}-{Guid.NewGuid():N}@test.ee",
            Notes = "Assignment contact"
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<Guid> CreateResidentAsync(IAppUOW uow)
    {
        var id = Guid.NewGuid();
        uow.Residents.Add(new ResidentDalDto
        {
            Id = id,
            ManagementCompanyId = TestTenants.CompanyAId,
            FirstName = "Assignment",
            LastName = "Resident",
            IdCode = $"ID{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            PreferredLanguage = "en"
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<Guid> ContactTypeIdAsync(AppDbContext db)
    {
        return await db.ContactTypes.AsNoTracking().Where(type => type.Code == "EMAIL").Select(type => type.Id).SingleAsync();
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
