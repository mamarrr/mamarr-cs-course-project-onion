using App.DAL.Contracts;
using App.DAL.DTO.Contacts;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class ContactRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ContactRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FindOptionsAndExistenceQueries_AreCompanyScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var contactTypeId = await ContactTypeIdAsync(db);
        var contactId = await CreateContactAsync(uow, contactTypeId, "find", "Find note");

        var found = await uow.Contacts.FindInCompanyAsync(contactId, TestTenants.CompanyAId);
        var exists = await uow.Contacts.ExistsInCompanyAsync(contactId, TestTenants.CompanyAId);
        var wrongCompany = await uow.Contacts.FindInCompanyAsync(contactId, Guid.NewGuid());
        var options = await uow.Contacts.OptionsByCompanyAsync(TestTenants.CompanyAId);

        found.Should().NotBeNull();
        found!.ContactTypeId.Should().Be(contactTypeId);
        found.Notes.Should().Be("Find note");
        exists.Should().BeTrue();
        wrongCompany.Should().BeNull();
        options.Should().Contain(contact => contact.Id == contactId);
    }

    [Fact]
    public async Task DuplicateValueExistsAsync_IsCompanyTypeAndContactScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var contactTypeId = await ContactTypeIdAsync(db);
        var contactId = await CreateContactAsync(uow, contactTypeId, "duplicate", "Duplicate note");
        var persisted = await uow.Contacts.FindInCompanyAsync(contactId, TestTenants.CompanyAId);

        var duplicate = await uow.Contacts.DuplicateValueExistsAsync(
            TestTenants.CompanyAId,
            contactTypeId,
            $" {persisted!.ContactValue.ToUpperInvariant()} ");
        var exceptSelf = await uow.Contacts.DuplicateValueExistsAsync(
            TestTenants.CompanyAId,
            contactTypeId,
            persisted.ContactValue,
            contactId);

        duplicate.Should().BeTrue();
        exceptSelf.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureNotesAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var contactTypeId = await ContactTypeIdAsync(db);
        var contactId = await CreateContactAsync(uow, contactTypeId, "update", "English note");

        var entity = await db.Contacts.AsTracking().SingleAsync(contact => contact.Id == contactId);
        entity.Notes!.SetTranslation("Estonian note", "et");
        db.Entry(entity).Property(contact => contact.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Contacts.UpdateAsync(new ContactDalDto
        {
            Id = contactId,
            ManagementCompanyId = TestTenants.CompanyAId,
            ContactTypeId = contactTypeId,
            ContactValue = UniqueEmail("updated"),
            Notes = "English updated"
        }, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.Contacts.AsNoTracking().SingleAsync(contact => contact.Id == contactId);
        persisted.Notes!.Translate("en").Should().Be("English updated");
        persisted.Notes.Translate("et").Should().Be("Estonian note");
    }

    [Fact]
    public async Task HasDependenciesAsync_ReturnsFalseForUnlinkedContact()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var contactId = await CreateContactAsync(uow, await ContactTypeIdAsync(db), "dependencies", "No links");

        var hasDependencies = await uow.Contacts.HasDependenciesAsync(contactId, TestTenants.CompanyAId);

        hasDependencies.Should().BeFalse();
    }

    private static async Task<Guid> CreateContactAsync(IAppUOW uow, Guid contactTypeId, string suffix, string notes)
    {
        var id = Guid.NewGuid();
        uow.Contacts.Add(new ContactDalDto
        {
            Id = id,
            ManagementCompanyId = TestTenants.CompanyAId,
            ContactTypeId = contactTypeId,
            ContactValue = UniqueEmail(suffix),
            Notes = notes
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<Guid> ContactTypeIdAsync(AppDbContext db)
    {
        return await db.ContactTypes
            .AsNoTracking()
            .Where(type => type.Code == "EMAIL")
            .Select(type => type.Id)
            .SingleAsync();
    }

    private static string UniqueEmail(string suffix)
    {
        return $"{suffix}-{Guid.NewGuid():N}@test.ee";
    }
}
